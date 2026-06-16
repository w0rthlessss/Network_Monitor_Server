"""
Two-stage feature selection for CICIDS2017 feature vectors.

Stage 1 — VarianceThreshold:
    Drops features whose variance on the training set falls below the
    threshold.  In CICIDS2017 this reliably removes the bulk-rate columns
    (Fwd/Bwd Avg Bytes/Bulk, Fwd/Bwd Avg Packets/Bulk, Fwd/Bwd Avg Bulk Rate)
    that are almost always 0 and carry no predictive signal.

Stage 2 — Greedy correlation filter:
    From every pair of features with Pearson |r| > threshold, drops the
    feature with the highest mean absolute correlation to the rest of the
    surviving set.  Variance filtering runs first so the correlation matrix
    is computed only on the features that already passed stage 1.

Both stages are fit on the training set only.  The resulting boolean mask is
saved to disk and loaded at startup so inference applies the identical
transformation to each incoming flow vector.
"""

from pathlib import Path

import joblib
import numpy as np
from sklearn.feature_selection import VarianceThreshold

from capture.feature_extractor import FEATURE_NAMES


class FeatureSelector:
    """
    Fit on training data, then transform batches (training) or single
    vectors (inference) with the same feature mask.

    Training workflow:
        X_train_sel = selector.fit_transform(X_train)
        X_test_sel  = selector.transform_batch(X_test)
        selector.save(path)

    Inference workflow:
        selector.load(path)
        selected = selector.transform_single(raw_features)
    """

    def __init__(
        self,
        variance_threshold: float = 0.01,
        correlation_threshold: float = 0.85,
    ) -> None:
        self._var_threshold = variance_threshold
        self._corr_threshold = correlation_threshold
        self._mask: np.ndarray | None = None       # bool, shape (n_original,)
        self._selected_names: list[str] = []

    # ------------------------------------------------------------------
    # Training API

    def fit_transform(self, X: np.ndarray) -> np.ndarray:
        """
        Fit both stages on *X* (training set) and return the filtered matrix.
        """
        var_mask = self._fit_variance(X)
        X_var = X[:, var_mask]

        corr_mask = self._fit_correlation(X_var)

        # Map local corr_mask indices back to original feature space
        var_indices = np.where(var_mask)[0]
        kept_indices = var_indices[corr_mask]

        self._mask = np.zeros(len(FEATURE_NAMES), dtype=bool)
        self._mask[kept_indices] = True
        self._selected_names = [FEATURE_NAMES[i] for i in kept_indices]

        print(
            f"[selector] {len(FEATURE_NAMES)} -> "
            f"{var_mask.sum()} (variance) -> "
            f"{self._mask.sum()} (correlation)"
        )
        return X[:, self._mask]

    def transform_batch(self, X: np.ndarray) -> np.ndarray:
        """Apply the fitted mask to a batch (e.g. test / validation set)."""
        self._check_fitted()
        return X[:, self._mask]

    # ------------------------------------------------------------------
    # Inference API

    def transform_single(self, features: list[float]) -> list[float]:
        """Apply the fitted mask to a single flow's feature vector."""
        self._check_fitted()
        return np.array(features, dtype=np.float64)[self._mask].tolist()

    # ------------------------------------------------------------------
    # Persistence

    def save(self, path: str) -> None:
        """Save the mask and feature names to *path*."""
        self._check_fitted()
        Path(path).parent.mkdir(parents=True, exist_ok=True)
        joblib.dump({"mask": self._mask, "names": self._selected_names}, path)

    def load(self, path: str) -> None:
        """Load a previously saved mask from *path*."""
        data = joblib.load(path)
        self._mask = data["mask"]
        self._selected_names = data["names"]

    # ------------------------------------------------------------------
    # Introspection

    @property
    def is_fitted(self) -> bool:
        return self._mask is not None

    @property
    def n_features_out(self) -> int:
        self._check_fitted()
        return int(self._mask.sum())

    @property
    def selected_feature_names(self) -> list[str]:
        self._check_fitted()
        return list(self._selected_names)

    # ------------------------------------------------------------------
    # Private helpers

    def _fit_variance(self, X: np.ndarray) -> np.ndarray:
        vt = VarianceThreshold(threshold=self._var_threshold)
        vt.fit(X)
        return vt.get_support()

    def _fit_correlation(self, X: np.ndarray) -> np.ndarray:
        """
        Greedy Pearson correlation filter.

        Repeatedly removes the feature with the highest mean |r| to all
        other surviving features until no pair exceeds the threshold.
        The correlation matrix is computed once upfront; subsequent
        iterations reuse it by masking rows/columns.
        """
        corr = np.abs(np.corrcoef(X.T))
        np.fill_diagonal(corr, 0.0)

        n = corr.shape[0]
        keep = np.ones(n, dtype=bool)

        while True:
            active = np.where(keep)[0]
            if len(active) < 2:
                break

            sub = corr[np.ix_(active, active)]
            if sub.max() <= self._corr_threshold:
                break

            # Drop the feature with the highest mean correlation to others
            worst_local = np.argmax(sub.mean(axis=0))
            keep[active[worst_local]] = False

        return keep

    def _check_fitted(self) -> None:
        if self._mask is None:
            raise RuntimeError("FeatureSelector has not been fitted or loaded")


selector = FeatureSelector()
