import json
from typing import Any, Optional

import torch

from model.network_lstm import NetworkLSTM
from model.train_model import train as _train


def load(model_path: str) -> Any:
    """Load a saved NetworkLSTM checkpoint. Attaches window_size as attribute."""
    checkpoint = torch.load(model_path, map_location="cpu", weights_only=False)
    cfg   = checkpoint["config"]
    model = NetworkLSTM(
        input_size  = cfg["n_features"],
        hidden_size = cfg["hidden_size"],
        dropout     = cfg["dropout"],
    )
    model.load_state_dict(checkpoint["state_dict"])
    model.eval()
    model.window_size = cfg["window_size"]
    model.threshold   = cfg.get("threshold", 0.5)
    from config import THRESHOLD_OVERRIDE
    if THRESHOLD_OVERRIDE > 0:
        model.threshold = THRESHOLD_OVERRIDE
        print(f"[trainer] threshold overridden: {THRESHOLD_OVERRIDE}")
    return model


def train(
    base_model: Optional[Any],
    hyperparameters: dict,
    save_path: str,
) -> tuple[Any, str, str]:
    """
    Train or retrain a model.  Called from routers.py via asyncio.to_thread.

    Args:
        base_model:      ignored (full retraining on each call)
        hyperparameters: dict from HyperparametersPayload.model_dump()
        save_path:       where to write the model checkpoint (derived from outputName)

    Returns:
        trained_model — NetworkLSTM ready for inference
        metrics_json  — JSON string with accuracy / precision / recall / f1 / roc_auc
        saved_path    — same as save_path
    """
    hp = hyperparameters
    metrics = _train(
        save_path    = save_path,
        max_epochs   = hp.get("epochs", 50),
        batch_size   = hp.get("batchSize", 512),
        hidden_size  = hp.get("hiddenSize", 128),
        dropout      = hp.get("dropout", 0.4),
        lr           = hp.get("learningRate", 1e-3),
        weight_decay = hp.get("weightDecay", 0.0),
        patience          = hp.get("earlyStoppingPatience", 5),
        pos_weight_factor = hp.get("posWeightFactor", 1.0),
        target_recall     = hp.get("targetRecall", 0.0),
    )
    trained_model = load(save_path)
    return trained_model, json.dumps(metrics), save_path
