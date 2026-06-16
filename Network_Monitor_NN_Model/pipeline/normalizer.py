import numpy as np
import joblib
from sklearn.preprocessing import StandardScaler


def _sanitize(arr: np.ndarray) -> np.ndarray:
    """Replace nan / ±inf with 0.0 before scaling."""
    return np.nan_to_num(arr, nan=0.0, posinf=0.0, neginf=0.0)


class Normalizer:
    """
    Wraps sklearn StandardScaler for CICIDS2017 feature vectors.

    Training workflow:
        X_train_norm = normalizer.fit_transform(X_train)  # → np.ndarray
        X_test_norm  = normalizer.transform_batch(X_test)  # → np.ndarray
        normalizer.save(path)

    Inference workflow:
        normalizer.load(path)
        norm_vec = normalizer.transform(raw_features)  # → list[float]
    """

    def __init__(self) -> None:
        self._scaler: StandardScaler | None = None

    # ------------------------------------------------------------------
    # Training API — operates on full arrays

    def fit(self, X: np.ndarray) -> None:
        """Fit the scaler on the training set."""
        self._scaler = StandardScaler()
        self._scaler.fit(_sanitize(X.astype(np.float64)))

    def fit_transform(self, X: np.ndarray) -> np.ndarray:
        """Fit and transform the training set. Returns float64 ndarray."""
        arr = _sanitize(X.astype(np.float64))
        self._scaler = StandardScaler()
        return self._scaler.fit_transform(arr)

    def transform_batch(self, X: np.ndarray) -> np.ndarray:
        """Transform the test / validation set. Returns float64 ndarray."""
        if self._scaler is None:
            raise RuntimeError("Normalizer has not been fitted or loaded")
        return self._scaler.transform(_sanitize(X.astype(np.float64)))

    # ------------------------------------------------------------------
    # Inference API — operates on a single feature vector

    def transform(self, features: list[float]) -> list[float]:
        """Scale one feature vector. Returns a plain Python list."""
        if self._scaler is None:
            raise RuntimeError("Normalizer has not been fitted or loaded")
        arr = _sanitize(np.array([features], dtype=np.float64))
        return self._scaler.transform(arr)[0].tolist()

    # ------------------------------------------------------------------

    def save(self, path: str) -> None:
        """Persist the fitted scaler to *path*."""
        if self._scaler is None:
            raise RuntimeError("Normalizer has not been fitted")
        joblib.dump(self._scaler, path)

    def load(self, path: str) -> None:
        """Load a previously saved scaler from *path*."""
        self._scaler = joblib.load(path)

    @property
    def is_fitted(self) -> bool:
        return self._scaler is not None


normalizer = Normalizer()
