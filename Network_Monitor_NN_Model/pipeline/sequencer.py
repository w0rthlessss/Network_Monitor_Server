from collections import deque

import numpy as np


class Sequencer:
    """
    Sliding window for online inference — accumulates normalised feature
    vectors one flow at a time.

    Used when the model expects a sequence of consecutive flows as input
    (LSTM / Transformer).  Call push() for each new flow; when ready is True
    the window is full and get_sequence() returns the current batch.

    The deque slides by one on every push(), so the window always reflects
    the most recent window_size flows.
    """

    def __init__(self, window_size: int = 10) -> None:
        self.window_size = window_size
        self._buffer: deque[list[float]] = deque(maxlen=window_size)

    def push(self, features: list[float]) -> None:
        """Add a normalised feature vector to the sliding window."""
        self._buffer.append(features)

    @property
    def ready(self) -> bool:
        """True when the window is full and a prediction can be made."""
        return len(self._buffer) == self.window_size

    def get_sequence(self) -> list[list[float]]:
        """Return the current window as a list of feature vectors."""
        return list(self._buffer)

    def reset(self, window_size: int | None = None) -> None:
        if window_size is not None:
            self.window_size = window_size
        self._buffer = deque(maxlen=self.window_size)


# Module-level singleton for inference — shared across all _on_flow tasks.
sequencer = Sequencer()


def make_sequences(
    X: np.ndarray,
    y: np.ndarray,
    window_size: int = 10,
) -> tuple[np.ndarray, np.ndarray]:
    """
    Slice (X, y) into overlapping sliding-window sequences for LSTM training.

    Label strategy: a sequence is labelled 1 (attack) if ANY flow inside the
    window is an attack.  This is the conservative choice for an IDS — missing
    a single malicious flow is worse than an extra false positive.

    Args:
        X:           float64 array of shape (n_flows, n_features)
        y:           int8 array of shape (n_flows,);  0 = BENIGN, 1 = attack
        window_size: number of consecutive flows per sequence

    Returns:
        X_seq:  float64 array of shape (n_sequences, window_size, n_features)
        y_seq:  int8 array of shape (n_sequences,)
    """
    n = len(X)
    n_seq = n - window_size + 1
    if n_seq <= 0:
        raise ValueError(
            f"Dataset has {n} rows which is less than window_size={window_size}"
        )

    # Build (n_seq, window_size, n_features) without Python loops:
    # indices[i] selects rows i … i+window_size-1
    indices = np.arange(window_size)[None, :] + np.arange(n_seq)[:, None]
    X_seq = X[indices]

    # A sequence is an attack if any of its window_size labels equals 1
    y_seq = y[indices].any(axis=1).astype(np.int8)

    return X_seq, y_seq
