from typing import Any, Optional


class _ActiveModel:

    def __init__(self) -> None:
        self.model_id:   Optional[int] = None
        self.model_path: Optional[str] = None
        self.model:      Optional[Any] = None

    @property
    def is_loaded(self) -> bool:
        return self.model is not None

    def set(self, model_id: int, model_path: str, model: Any) -> None:
        self.model_id   = model_id
        self.model_path = model_path
        self.model      = model

    def clear(self) -> None:
        self.model_id   = None
        self.model_path = None
        self.model      = None


active_model = _ActiveModel()
