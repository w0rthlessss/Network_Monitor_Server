import asyncio
import os
from contextlib import asynccontextmanager
from datetime import datetime, timezone
from pathlib import Path

from fastapi import FastAPI

import api_client
import system_usage
from capture import feature_extractor, sniffer
from config import BYPASS_MODEL, DEFAULT_USER_ID, SYSTEM_USAGE_INTERVAL
from model import predictor, state as model_state, trainer
from pipeline import artifact_path
from pipeline import normalizer as norm_module
from pipeline import feature_selector as fs_module
from pipeline.sequencer import sequencer as _sequencer
from routers import router
from schemas import CreateModelPayload, PredictionPayload, TrafficRecordPayload

DEFAULT_MODEL_PATH = "models/model.pt"


async def _on_flow(flow_dict: dict) -> None:
    print(f"[flow] received: {flow_dict.get('src_ip','?')}:{flow_dict.get('src_port','?')} -> {flow_dict.get('dst_ip','?')}:{flow_dict.get('dst_port','?')}")
    try:
        await _process_flow(flow_dict)
    except Exception as exc:
        print(f"[flow] error: {exc}")


async def _process_flow(flow_dict: dict) -> None:
    conn_payload, raw_features = feature_extractor.to_connection_payload(flow_dict)

    if BYPASS_MODEL:
        prediction = PredictionPayload(
            modelId=model_state.active_model.model_id or 0,
            result=False,
            confidence=1.0,
            topFeature="bypass",
            timestamp=datetime.now(timezone.utc),
        )
        await api_client.post_traffic(
            TrafficRecordPayload(connection=conn_payload, prediction=prediction)
        )
        return

    if not model_state.active_model.is_loaded:
        print("[flow] no model loaded, skipping")
        return

    selected_features = fs_module.selector.transform_single(raw_features)
    normalized = await asyncio.to_thread(
        norm_module.normalizer.transform, selected_features
    )
    _sequencer.push(normalized)
    print(f"[flow] sequencer {len(_sequencer._buffer)}/{_sequencer.window_size}")
    if not _sequencer.ready:
        return

    result, confidence, top_feature = await asyncio.to_thread(
        predictor.predict, model_state.active_model.model, _sequencer.get_sequence()
    )

    prediction = PredictionPayload(
        modelId=model_state.active_model.model_id,
        result=result,
        confidence=confidence,
        topFeature=top_feature,
        timestamp=datetime.now(timezone.utc),
    )

    await api_client.post_traffic(
        TrafficRecordPayload(
            connection=conn_payload,
            prediction=prediction,
            alertDescription=None,
        )
    )


def _spawn_flow_task(flow_dict: dict) -> None:
    """Sync callback passed to sniffer — spawns an independent Task per flow."""
    asyncio.create_task(_on_flow(flow_dict))


@asynccontextmanager
async def lifespan(app: FastAPI):
    await api_client.init()

    active = await api_client.get_active_model()
    if active:
        try:
            model_obj = trainer.load(active.modelPath)
            norm_module.normalizer.load(artifact_path(active.modelPath, "_scaler"))
            fs_module.selector.load(artifact_path(active.modelPath, "_selector"))
            _sequencer.reset(window_size=getattr(model_obj, "window_size", 10))
            model_state.active_model.set(active.id, active.modelPath, model_obj)
            print(f"[startup] active model restored: id={active.id}")
        except Exception as exc:
            print(f"[startup] failed to load model artifacts: {exc}")
    elif Path(DEFAULT_MODEL_PATH).exists():
        print(f"[startup] no active model in DB, trying default: {DEFAULT_MODEL_PATH}")
        try:
            model_obj = trainer.load(DEFAULT_MODEL_PATH)
            norm_module.normalizer.load(artifact_path(DEFAULT_MODEL_PATH, "_scaler"))
            fs_module.selector.load(artifact_path(DEFAULT_MODEL_PATH, "_selector"))
            _sequencer.reset(window_size=getattr(model_obj, "window_size", 10))
            model_id = 0
            try:
                result = await api_client.register_model(
                    CreateModelPayload(userId=DEFAULT_USER_ID, metrics="{}", modelPath=DEFAULT_MODEL_PATH)
                )
                model_id = result["id"]
                print(f"[startup] default model registered: id={model_id}")
            except Exception as reg_exc:
                print(f"[startup] DB registration failed (running without DB record): {reg_exc}")
            model_state.active_model.set(model_id, DEFAULT_MODEL_PATH, model_obj)
            print(f"[startup] default model activated (id={model_id})")
        except Exception as exc:
            print(f"[startup] failed to load default model: {exc}")
    else:
        print("[startup] no active model found")

    interface = os.getenv("CAPTURE_INTERFACE", "eth0")
    tasks = [
        asyncio.create_task(
            system_usage.run(api_client.post_system_usage, interval=SYSTEM_USAGE_INTERVAL)
        ),
        asyncio.create_task(
            sniffer.run_live(interface, _spawn_flow_task)
        ),
    ]

    yield

    for task in tasks:
        task.cancel()
    await api_client.close()


app = FastAPI(lifespan=lifespan)
app.include_router(router)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=False)
