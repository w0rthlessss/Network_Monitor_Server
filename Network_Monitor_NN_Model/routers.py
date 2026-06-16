import asyncio
from typing import Optional

from fastapi import APIRouter

import api_client
from model import state as model_state, trainer
from pipeline import artifact_path
from pipeline import normalizer as norm_module
from pipeline import feature_selector as fs_module
from pipeline.sequencer import sequencer as _sequencer
from schemas import CreateModelPayload, SetActiveModelRequest, TrainRequest, UpdateModelPayload

router = APIRouter()


def _load_artifacts(model_path: str) -> object:
    """Load model + scaler + selector from sibling paths. Returns model object."""
    model_obj = trainer.load(model_path)
    norm_module.normalizer.load(artifact_path(model_path, "_scaler"))
    fs_module.selector.load(artifact_path(model_path, "_selector"))
    _sequencer.reset(window_size=getattr(model_obj, "window_size", 10))
    return model_obj


@router.post("/set-active-model")
async def set_active_model(request: SetActiveModelRequest):
    model_obj = await asyncio.to_thread(_load_artifacts, request.modelPath)
    model_state.active_model.set(request.modelId, request.modelPath, model_obj)
    return {"status": "ok"}


@router.post("/train")
async def train(request: TrainRequest):
    hp = request.hyperparameters
    save_path = f"models/{hp.outputName}.pt"
    asyncio.create_task(_run_training(request.modelId, request.userId, hp.model_dump(), save_path))
    return {"status": "accepted"}


async def _run_training(
    model_id: Optional[int],
    user_id: int,
    hyperparameters: dict,
    save_path: str,
) -> None:
    base = model_state.active_model.model if model_id == model_state.active_model.model_id else None
    try:
        trained_model, metrics_json, saved_path = await asyncio.to_thread(
            trainer.train, base, hyperparameters, save_path
        )

        # Reload pipeline artifacts for the new model
        norm_module.normalizer.load(artifact_path(saved_path, "_scaler"))
        fs_module.selector.load(artifact_path(saved_path, "_selector"))
        _sequencer.reset(window_size=getattr(trained_model, "window_size", 10))

        if model_id is None:
            result = await api_client.register_model(
                CreateModelPayload(userId=user_id, metrics=metrics_json, modelPath=saved_path)
            )
            model_state.active_model.set(result["id"], saved_path, trained_model)
            print(f"[train] new model registered: id={result['id']}")
        else:
            await api_client.update_model(
                model_id,
                UpdateModelPayload(metrics=metrics_json, modelPath=saved_path),
            )
            model_state.active_model.set(model_id, saved_path, trained_model)
            print(f"[train] model {model_id} updated")
    except Exception as exc:
        print(f"[train] error: {exc}")
