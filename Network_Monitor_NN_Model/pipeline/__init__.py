from pathlib import Path


def artifact_path(model_path: str, tag: str) -> str:
    """Derive a sibling artifact path from the model path.

    artifact_path('models/run1.pkl', '_scaler')   → 'models/run1_scaler.pkl'
    artifact_path('models/run1.pkl', '_selector') → 'models/run1_selector.pkl'
    """
    p = Path(model_path)
    return str(p.with_name(p.stem + tag + p.suffix))
