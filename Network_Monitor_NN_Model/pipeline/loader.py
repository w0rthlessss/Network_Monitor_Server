"""
Loads and preprocesses the CICIDS2017 GeneratedLabelledFlows dataset.

Split strategy — day-based temporal split:
    Train : Monday + Tuesday + Wednesday + Thursday morning
            (BENIGN, Brute Force, DoS, Web Attacks)
    Val   : Thursday afternoon
            (Infiltration — unseen attack type during training)
    Test  : Friday morning + afternoon
            (Botnet, DDoS, PortScan — unseen attack types)

Using a temporal split instead of random shuffling because:
  - LSTM requires temporally ordered sequences; random mixing produces
    sequences that span multiple days and have no real-world meaning.
  - Day-based split mirrors real deployment: train on historical data,
    evaluate on future traffic.  It also tests generalisation to attack
    types the model has never seen.

Preprocessing steps (adapted from cicids2017-comprehensive-data-processing-for-ml.ipynb):
  1. Strip leading/trailing whitespace from column names.
  2. Select feature columns in FEATURE_NAMES order.  Pandas auto-renames
     the duplicate "Fwd Header Length" column to "Fwd Header Length.1".
  3. Drop fully duplicate rows (especially prevalent in BENIGN traffic).
  4. Replace inf / -inf with NaN, then drop those rows.  Filling inf with 0
     is reserved for inference (normalizer._sanitize) where flows cannot be
     discarded.
  5. Clip Init_Win_bytes_forward / backward to 0  (-1 means "unknown").
  6. Encode Label: "BENIGN" → 0, any attack → 1.
"""

from pathlib import Path

import numpy as np
import pandas as pd

from capture.feature_extractor import FEATURE_NAMES
from config import DATA_SOURCE

_DATA_DIR = (
    Path(DATA_SOURCE)
    if DATA_SOURCE
    else Path(__file__).parent.parent / "data" / "GeneratedLabelledFlows" / "TrafficLabelling"
)

_CLIP_COLS = ("Init_Win_bytes_forward", "Init_Win_bytes_backward")

# ---------------------------------------------------------------------------
# Day-based file assignment
# ---------------------------------------------------------------------------

TRAIN_FILES = [
    "Monday-WorkingHours.pcap_ISCX.csv",
    "Tuesday-WorkingHours.pcap_ISCX.csv",
    "Wednesday-workingHours.pcap_ISCX.csv",
    "Thursday-WorkingHours-Morning-WebAttacks.pcap_ISCX.csv",
]

VAL_FILES = [
    "Thursday-WorkingHours-Afternoon-Infilteration.pcap_ISCX.csv",
]

TEST_FILES = [
    "Friday-WorkingHours-Morning.pcap_ISCX.csv",
    "Friday-WorkingHours-Afternoon-DDos.pcap_ISCX.csv",
    "Friday-WorkingHours-Afternoon-PortScan.pcap_ISCX.csv",
]

# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _read_csv(path: Path) -> pd.DataFrame:
    df = pd.read_csv(path, low_memory=False, encoding="latin-1")
    df.columns = df.columns.str.strip()
    return df


def _encode_labels(series: pd.Series) -> np.ndarray:
    return (series.str.strip() != "BENIGN").astype(np.int8).values


def _process(filenames: list[str]) -> tuple[np.ndarray, np.ndarray]:
    """
    Load, clean, and return (X, y) for a list of CSV filenames.

    Temporal order within the list is preserved — files are concatenated
    in the order given, which matches the chronological day order.
    """
    paths = [_DATA_DIR / f for f in filenames]
    missing = [p for p in paths if not p.exists()]
    if missing:
        raise FileNotFoundError(f"Dataset files not found: {missing}")

    frames = [_read_csv(p) for p in paths]
    df = pd.concat(frames, ignore_index=True)

    # Drop identical rows — especially common in BENIGN traffic
    df = df.drop_duplicates(keep="first")

    y = _encode_labels(df["Label"])

    X_df = df[FEATURE_NAMES].replace([np.inf, -np.inf], np.nan)
    for col in _CLIP_COLS:
        if col in X_df.columns:
            X_df[col] = X_df[col].clip(lower=0)

    # Drop rows that still contain NaN (were inf before replace)
    mask = X_df.notna().all(axis=1)
    X = X_df[mask].to_numpy(dtype=np.float64)
    y = y[mask]

    return X, y


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

Split = tuple[np.ndarray, np.ndarray]   # (X, y)


def load() -> tuple[Split, Split, Split]:
    """
    Load the CICIDS2017 dataset with a day-based temporal split.

    Returns:
        (X_train, y_train) — shape (n, 77),  days Mon–Thu morning
        (X_val,   y_val)   — shape (n, 77),  Thu afternoon
        (X_test,  y_test)  — shape (n, 77),  Friday

    All arrays are float64 / int8.  No shuffling is applied so that
    make_sequences() produces temporally valid sliding windows.

    Typical usage in trainer.py:
        (X_train, y_train), (X_val, y_val), (X_test, y_test) = load()
    """
    return (
        _process(TRAIN_FILES),
        _process(VAL_FILES),
        _process(TEST_FILES),
    )
