"""
Standalone LSTM training script for network intrusion detection.

Run from Network_Monitor_NN_Model/:
    python -m model.train_model
    python -m model.train_model --save-path models/model.pt --window-size 10
"""

import argparse
import json
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn
from sklearn.metrics import classification_report, confusion_matrix, f1_score, precision_recall_curve, roc_auc_score
from torch.utils.data import DataLoader, TensorDataset

from model.network_lstm import NetworkLSTM
from pipeline import artifact_path
from pipeline.feature_selector import selector
from pipeline.loader import load
from pipeline.normalizer import normalizer
from pipeline.sequencer import make_sequences


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_loader(
    X: np.ndarray,
    y: np.ndarray,
    batch_size: int,
    shuffle: bool,
) -> DataLoader:
    Xt = torch.tensor(X, dtype=torch.float32)
    yt = torch.tensor(y, dtype=torch.float32)
    return DataLoader(TensorDataset(Xt, yt), batch_size=batch_size, shuffle=shuffle)


def _pos_weight(y: np.ndarray, factor: float = 1.0) -> torch.Tensor:
    n_neg = int((y == 0).sum())
    n_pos = int((y == 1).sum())
    return torch.tensor([factor * n_neg / max(n_pos, 1)], dtype=torch.float32)


def _eval_epoch(
    model: nn.Module,
    loader: DataLoader,
    criterion: nn.Module,
    device: torch.device,
) -> tuple[float, float]:
    model.eval()
    total_loss = 0.0
    correct = 0
    total = 0
    with torch.no_grad():
        for Xb, yb in loader:
            Xb, yb = Xb.to(device), yb.to(device)
            logits = model(Xb)
            total_loss += criterion(logits, yb).item() * len(Xb)
            preds = (torch.sigmoid(logits) >= 0.5).float()
            correct += (preds == yb).sum().item()
            total += len(yb)
    return total_loss / total, correct / total


# ---------------------------------------------------------------------------
# Main training function (also called by trainer.py via API)
# ---------------------------------------------------------------------------

def train(
    save_path: str = "models/model.pt",
    window_size: int = 10,
    hidden_size: int = 128,
    dropout: float = 0.4,
    lr: float = 1e-3,
    weight_decay: float = 0.0,
    batch_size: int = 512,
    max_epochs: int = 50,
    patience: int = 5,
    pos_weight_factor: float = 1.0,
    target_recall: float = 0.0,
) -> dict:
    """
    Full training pipeline:
        load → feature selection → normalization → sequences → train → evaluate → save

    Returns:
        metrics dict with accuracy, precision, recall, f1, roc_auc
    Saves:
        save_path           — model checkpoint
        save_path _selector — feature selector mask
        save_path _scaler   — StandardScaler parameters
    """
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"[train] device: {device}")

    # 1. Load — day-based temporal split, order preserved ----------------
    print("[train] loading dataset ...")
    (X_train, y_train), (X_val, y_val), (X_test, y_test) = load()
    print(
        f"[train] rows — "
        f"train={len(X_train):,}  val={len(X_val):,}  test={len(X_test):,}"
    )

    # 2. Feature selection — fit on training set only --------------------
    print("[train] selecting features ...")
    X_train = selector.fit_transform(X_train)
    X_val   = selector.transform_batch(X_val)
    X_test  = selector.transform_batch(X_test)
    n_features = X_train.shape[1]
    print(f"[train] features after selection: {n_features}")

    # 3. Normalization — fit on training set only ------------------------
    print("[train] normalizing ...")
    X_train = normalizer.fit_transform(X_train)
    X_val   = normalizer.transform_batch(X_val)
    X_test  = normalizer.transform_batch(X_test)

    # 4. Build sliding-window sequences (per split, no cross-day mixing) -
    print("[train] building sequences ...")
    X_train, y_train = make_sequences(X_train, y_train, window_size)
    X_val,   y_val   = make_sequences(X_val,   y_val,   window_size)
    X_test,  y_test  = make_sequences(X_test,  y_test,  window_size)
    print(
        f"[train] sequences — "
        f"train={len(X_train):,}  val={len(X_val):,}  test={len(X_test):,}"
    )
    print(f"[train] input shape: {X_train.shape}  (samples, window, features)")

    # 5. DataLoaders ------------------------------------------------------
    train_loader = _make_loader(X_train, y_train, batch_size, shuffle=True)
    val_loader   = _make_loader(X_val,   y_val,   batch_size, shuffle=False)
    test_loader  = _make_loader(X_test,  y_test,  batch_size, shuffle=False)

    # 6. Model, loss, optimizer -------------------------------------------
    model     = NetworkLSTM(n_features, hidden_size, dropout).to(device)
    pw        = _pos_weight(y_train, pos_weight_factor).to(device)
    criterion = nn.BCEWithLogitsLoss(pos_weight=pw)
    optimizer = torch.optim.Adam(model.parameters(), lr=lr, weight_decay=weight_decay)

    print(f"[train] parameters : {sum(p.numel() for p in model.parameters()):,}")
    print(f"[train] pos_weight : {pw.item():.2f}")

    # 7. Training loop with early stopping --------------------------------
    best_val_loss = float("inf")
    best_state    = None
    no_improve    = 0

    history = {"train_loss": [], "train_acc": [], "val_loss": [], "val_acc": []}

    for epoch in range(1, max_epochs + 1):
        model.train()
        train_loss = 0.0
        train_correct = 0
        train_total = 0
        for Xb, yb in train_loader:
            Xb, yb = Xb.to(device), yb.to(device)
            optimizer.zero_grad()
            logits = model(Xb)
            loss = criterion(logits, yb)
            loss.backward()
            optimizer.step()
            train_loss += loss.item() * len(Xb)
            preds = (torch.sigmoid(logits.detach()) >= 0.5).float()
            train_correct += (preds == yb).sum().item()
            train_total += len(yb)
        train_loss /= train_total
        train_acc = train_correct / train_total

        val_loss, val_acc = _eval_epoch(model, val_loader, criterion, device)

        history["train_loss"].append(round(train_loss, 6))
        history["train_acc"].append(round(train_acc, 6))
        history["val_loss"].append(round(val_loss, 6))
        history["val_acc"].append(round(val_acc, 6))

        print(
            f"[epoch {epoch:02d}/{max_epochs}] "
            f"train_loss={train_loss:.4f}  train_acc={train_acc:.4f}  "
            f"val_loss={val_loss:.4f}  val_acc={val_acc:.4f}"
        )

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_state    = {k: v.cpu().clone() for k, v in model.state_dict().items()}
            no_improve    = 0
        else:
            no_improve += 1
            if no_improve >= patience:
                print(f"[train] early stopping at epoch {epoch}")
                break

    model.load_state_dict(best_state)

    # 8. Test evaluation --------------------------------------------------
    model.eval()
    all_probs, all_labels = [], []
    with torch.no_grad():
        for Xb, yb in test_loader:
            probs = torch.sigmoid(model(Xb.to(device))).cpu().numpy()
            all_probs.extend(probs.tolist())
            all_labels.extend(yb.numpy().tolist())

    all_probs  = np.array(all_probs)
    all_labels = np.array(all_labels, dtype=int)

    precisions, recalls, thresholds = precision_recall_curve(all_labels, all_probs)

    if target_recall > 0.0:
        # Highest threshold where recall >= target_recall (maximises precision)
        valid = recalls[:-1] >= target_recall
        if valid.any():
            best_threshold = float(thresholds[valid.nonzero()[0][-1]])
        else:
            best_threshold = float(thresholds[0])
            print(f"[train] warning: target_recall={target_recall} unreachable, using min threshold")
    else:
        # Default: maximise F1 for ATTACK class
        f1_scores = 2 * precisions[:-1] * recalls[:-1] / (precisions[:-1] + recalls[:-1] + 1e-8)
        best_threshold = float(thresholds[f1_scores.argmax()])

    print(f"[train] optimal threshold: {best_threshold:.4f}")

    all_preds  = (all_probs >= best_threshold).astype(int)

    report  = classification_report(
        all_labels, all_preds,
        target_names=["BENIGN", "ATTACK"],
        output_dict=True,
    )
    roc_auc = roc_auc_score(all_labels, all_probs)

    print("\n[test results]")
    print(classification_report(all_labels, all_preds, target_names=["BENIGN", "ATTACK"]))
    print(f"ROC-AUC : {roc_auc:.4f}")
    print(f"Confusion matrix:\n{confusion_matrix(all_labels, all_preds)}")

    metrics = {
        "accuracy":  round(report["accuracy"], 4),
        "precision": round(report["ATTACK"]["precision"], 4),
        "recall":    round(report["ATTACK"]["recall"], 4),
        "f1":        round(report["ATTACK"]["f1-score"], 4),
        "roc_auc":   round(roc_auc, 4),
    }

    # 9. Save artifacts ---------------------------------------------------
    Path(save_path).parent.mkdir(parents=True, exist_ok=True)

    torch.save(
        {
            "state_dict": model.state_dict(),
            "config": {
                "n_features":  n_features,
                "hidden_size": hidden_size,
                "dropout":     dropout,
                "window_size": window_size,
                "threshold":   best_threshold,
            },
        },
        save_path,
    )
    selector.save(artifact_path(save_path, "_selector"))
    normalizer.save(artifact_path(save_path, "_scaler"))

    history_path = str(Path(save_path).with_name(Path(save_path).stem + "_history.json"))
    with open(history_path, "w") as f:
        json.dump(history, f)

    print(f"\n[train] saved → {save_path}")
    return metrics


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Train NetworkLSTM on CICIDS2017")
    parser.add_argument("--save-path",   default="models/model.pt")
    parser.add_argument("--window-size",  type=int,   default=10)
    parser.add_argument("--hidden-size",  type=int,   default=128)
    parser.add_argument("--dropout",      type=float, default=0.4)
    parser.add_argument("--lr",           type=float, default=1e-3)
    parser.add_argument("--weight-decay", type=float, default=0.0)
    parser.add_argument("--batch-size",   type=int,   default=512)
    parser.add_argument("--epochs",            type=int,   default=50)
    parser.add_argument("--patience",          type=int,   default=5)
    parser.add_argument("--pos-weight-factor", type=float, default=1.0)
    parser.add_argument("--target-recall",     type=float, default=0.0,
                        help="Minimum ATTACK recall to target (0 = maximise F1)")
    args = parser.parse_args()

    train(
        save_path         = args.save_path,
        window_size       = args.window_size,
        hidden_size       = args.hidden_size,
        dropout           = args.dropout,
        lr                = args.lr,
        weight_decay      = args.weight_decay,
        batch_size        = args.batch_size,
        max_epochs        = args.epochs,
        patience          = args.patience,
        pos_weight_factor = args.pos_weight_factor,
        target_recall     = args.target_recall,
    )
