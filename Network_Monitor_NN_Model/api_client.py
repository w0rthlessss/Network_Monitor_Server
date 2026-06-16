from typing import Optional

import httpx

from config import ASPNET_BASE_URL, INTERNAL_API_KEY
from schemas import (
    ActiveModelResponse,
    CreateModelPayload,
    SystemUsagePayload,
    TrafficRecordPayload,
    UpdateModelPayload,
)

_client: Optional[httpx.AsyncClient] = None


async def init() -> None:
    global _client
    _client = httpx.AsyncClient(
        base_url=ASPNET_BASE_URL,
        headers={"X-Api-Key": INTERNAL_API_KEY},
        timeout=30.0,
        trust_env=False,
    )


async def close() -> None:
    if _client:
        await _client.aclose()


async def get_active_model() -> Optional[ActiveModelResponse]:
    r = await _client.get("/internal/active-model")
    if r.status_code == 200:
        return ActiveModelResponse.model_validate(r.json())
    return None


async def post_traffic(payload: TrafficRecordPayload) -> None:
    r = await _client.post("/internal/traffic", json=payload.model_dump(mode="json"))
    if not r.is_success:
        print(f"[post_traffic] {r.status_code}: {r.text[:500]}")
    r.raise_for_status()


async def post_system_usage(payload: SystemUsagePayload) -> None:
    await _client.post("/internal/system-usage", json=payload.model_dump(mode="json"))


async def register_model(payload: CreateModelPayload) -> dict:
    r = await _client.post("/internal/models", json=payload.model_dump(mode="json"))
    r.raise_for_status()
    return r.json()


async def update_model(model_id: int, payload: UpdateModelPayload) -> None:
    r = await _client.patch(
        f"/internal/models/{model_id}",
        json=payload.model_dump(mode="json"),
    )
    r.raise_for_status()
