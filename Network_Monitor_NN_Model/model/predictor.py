import torch

from pipeline.feature_selector import selector


def predict(model, features: list[list[float]]) -> tuple[bool, float, str]:
    """
    Run inference on a single normalised, selected feature sequence.

    Args:
        model:    NetworkLSTM instance (eval mode, on CPU)
        features: sliding window — list of window_size feature vectors,
                  each already passed through selector and normalizer

    Returns:
        result      — True = attack detected
        confidence  — sigmoid probability in [0.0, 1.0]
        top_feature — feature name with the highest absolute value in the
                      last flow of the window (proxy for most salient signal)
    """
    x = torch.tensor([features], dtype=torch.float32)  # (1, window, n_features)

    threshold = getattr(model, "threshold", 0.5)

    with torch.no_grad():
        logit = model(x)
        prob  = torch.sigmoid(logit).item()

    last_step   = torch.tensor(features[-1]).abs()
    top_idx     = int(last_step.argmax())
    feat_names  = selector.selected_feature_names
    top_feature = feat_names[top_idx] if top_idx < len(feat_names) else str(top_idx)

    result = bool(prob >= threshold)
    confidence = prob if result else (1.0 - prob)
    return result, confidence, top_feature
