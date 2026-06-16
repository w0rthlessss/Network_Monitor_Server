"""
Maps a cicflowmeter flow dict to the two data structures used downstream:

  1. ConnectionPayload  — sent to ASP.NET via POST /internal/traffic
  2. feature vector     — numeric list consumed by pipeline/normalizer → model/predictor

cicflowmeter produces one dict per completed flow.  Its keys are snake_case
versions of the CICIDS2017 column names.

CICIDS2017 note: "Fwd Header Length" appears twice in the original dataset
(columns 41 and 62).  Pandas renames the second occurrence to
"Fwd Header Length.1".  We replicate this behaviour so the feature vector
produced here matches the columns the model was trained on.
"""
import json
import socket
import struct
from datetime import datetime, timezone

from schemas import ConnectionPayload

# ---------------------------------------------------------------------------
# Metadata columns — used for ConnectionPayload, NOT part of the feature vector
# ---------------------------------------------------------------------------
_METADATA = {
    "Flow ID", "Source IP", "Source Port",
    "Destination IP", "Destination Port", "Protocol", "Timestamp",
}

# ---------------------------------------------------------------------------
# Exact CICIDS2017 feature names in dataset column order.
# These are the column headers the model is trained on (after dropping metadata
# and Label).  The second "Fwd Header Length" is the pandas-renamed duplicate.
# ---------------------------------------------------------------------------
FEATURE_NAMES: list[str] = [
    "Flow Duration",
    "Total Fwd Packets",
    "Total Backward Packets",
    "Total Length of Fwd Packets",
    "Total Length of Bwd Packets",
    "Fwd Packet Length Max",
    "Fwd Packet Length Min",
    "Fwd Packet Length Mean",
    "Fwd Packet Length Std",
    "Bwd Packet Length Max",
    "Bwd Packet Length Min",
    "Bwd Packet Length Mean",
    "Bwd Packet Length Std",
    "Flow Bytes/s",
    "Flow Packets/s",
    "Flow IAT Mean",
    "Flow IAT Std",
    "Flow IAT Max",
    "Flow IAT Min",
    "Fwd IAT Total",
    "Fwd IAT Mean",
    "Fwd IAT Std",
    "Fwd IAT Max",
    "Fwd IAT Min",
    "Bwd IAT Total",
    "Bwd IAT Mean",
    "Bwd IAT Std",
    "Bwd IAT Max",
    "Bwd IAT Min",
    "Fwd PSH Flags",
    "Bwd PSH Flags",
    "Fwd URG Flags",
    "Bwd URG Flags",
    "Fwd Header Length",        # first occurrence (column 41)
    "Bwd Header Length",
    "Fwd Packets/s",
    "Bwd Packets/s",
    "Min Packet Length",
    "Max Packet Length",
    "Packet Length Mean",
    "Packet Length Std",
    "Packet Length Variance",
    "FIN Flag Count",
    "SYN Flag Count",
    "RST Flag Count",
    "PSH Flag Count",
    "ACK Flag Count",
    "URG Flag Count",
    "CWE Flag Count",
    "ECE Flag Count",
    "Down/Up Ratio",
    "Average Packet Size",
    "Avg Fwd Segment Size",
    "Avg Bwd Segment Size",
    "Fwd Header Length.1",      # duplicate (column 62), same value as col 41
    "Fwd Avg Bytes/Bulk",
    "Fwd Avg Packets/Bulk",
    "Fwd Avg Bulk Rate",
    "Bwd Avg Bytes/Bulk",
    "Bwd Avg Packets/Bulk",
    "Bwd Avg Bulk Rate",
    "Subflow Fwd Packets",
    "Subflow Fwd Bytes",
    "Subflow Bwd Packets",
    "Subflow Bwd Bytes",
    "Init_Win_bytes_forward",
    "Init_Win_bytes_backward",
    "act_data_pkt_fwd",
    "min_seg_size_forward",
    "Active Mean",
    "Active Std",
    "Active Max",
    "Active Min",
    "Idle Mean",
    "Idle Std",
    "Idle Max",
    "Idle Min",
]

# ---------------------------------------------------------------------------
# Mapping: CICIDS2017 column name  →  cicflowmeter flow dict key
# "Fwd Header Length.1" maps to the same cicflowmeter key as "Fwd Header Length"
# because cicflowmeter computes it once; we just duplicate the value.
# ---------------------------------------------------------------------------
_CIC_KEY: dict[str, str] = {
    "Flow Duration":              "flow_duration",
    "Total Fwd Packets":          "tot_fwd_pkts",
    "Total Backward Packets":     "tot_bwd_pkts",
    "Total Length of Fwd Packets":"totlen_fwd_pkts",
    "Total Length of Bwd Packets":"totlen_bwd_pkts",
    "Fwd Packet Length Max":      "fwd_pkt_len_max",
    "Fwd Packet Length Min":      "fwd_pkt_len_min",
    "Fwd Packet Length Mean":     "fwd_pkt_len_mean",
    "Fwd Packet Length Std":      "fwd_pkt_len_std",
    "Bwd Packet Length Max":      "bwd_pkt_len_max",
    "Bwd Packet Length Min":      "bwd_pkt_len_min",
    "Bwd Packet Length Mean":     "bwd_pkt_len_mean",
    "Bwd Packet Length Std":      "bwd_pkt_len_std",
    "Flow Bytes/s":               "flow_byts_s",
    "Flow Packets/s":             "flow_pkts_s",
    "Flow IAT Mean":              "flow_iat_mean",
    "Flow IAT Std":               "flow_iat_std",
    "Flow IAT Max":               "flow_iat_max",
    "Flow IAT Min":               "flow_iat_min",
    "Fwd IAT Total":              "fwd_iat_tot",
    "Fwd IAT Mean":               "fwd_iat_mean",
    "Fwd IAT Std":                "fwd_iat_std",
    "Fwd IAT Max":                "fwd_iat_max",
    "Fwd IAT Min":                "fwd_iat_min",
    "Bwd IAT Total":              "bwd_iat_tot",
    "Bwd IAT Mean":               "bwd_iat_mean",
    "Bwd IAT Std":                "bwd_iat_std",
    "Bwd IAT Max":                "bwd_iat_max",
    "Bwd IAT Min":                "bwd_iat_min",
    "Fwd PSH Flags":              "fwd_psh_flags",
    "Bwd PSH Flags":              "bwd_psh_flags",
    "Fwd URG Flags":              "fwd_urg_flags",
    "Bwd URG Flags":              "bwd_urg_flags",
    "Fwd Header Length":          "fwd_header_len",
    "Bwd Header Length":          "bwd_header_len",
    "Fwd Packets/s":              "fwd_pkts_s",
    "Bwd Packets/s":              "bwd_pkts_s",
    "Min Packet Length":          "pkt_len_min",
    "Max Packet Length":          "pkt_len_max",
    "Packet Length Mean":         "pkt_len_mean",
    "Packet Length Std":          "pkt_len_std",
    "Packet Length Variance":     "pkt_len_var",
    "FIN Flag Count":             "fin_flag_cnt",
    "SYN Flag Count":             "syn_flag_cnt",
    "RST Flag Count":             "rst_flag_cnt",
    "PSH Flag Count":             "psh_flag_cnt",
    "ACK Flag Count":             "ack_flag_cnt",
    "URG Flag Count":             "urg_flag_cnt",
    "CWE Flag Count":             "cwe_flag_count",
    "ECE Flag Count":             "ece_flag_cnt",
    "Down/Up Ratio":              "down_up_ratio",
    "Average Packet Size":        "pkt_size_avg",
    "Avg Fwd Segment Size":       "fwd_seg_size_avg",
    "Avg Bwd Segment Size":       "bwd_seg_size_avg",
    "Fwd Header Length.1":        "fwd_header_len",   # same key — duplicate value
    "Fwd Avg Bytes/Bulk":         "fwd_byts_b_avg",
    "Fwd Avg Packets/Bulk":       "fwd_pkts_b_avg",
    "Fwd Avg Bulk Rate":          "fwd_blk_rate_avg",
    "Bwd Avg Bytes/Bulk":         "bwd_byts_b_avg",
    "Bwd Avg Packets/Bulk":       "bwd_pkts_b_avg",
    "Bwd Avg Bulk Rate":          "bwd_blk_rate_avg",
    "Subflow Fwd Packets":        "subflow_fwd_pkts",
    "Subflow Fwd Bytes":          "subflow_fwd_byts",
    "Subflow Bwd Packets":        "subflow_bwd_pkts",
    "Subflow Bwd Bytes":          "subflow_bwd_byts",
    "Init_Win_bytes_forward":     "init_fwd_win_byts",
    "Init_Win_bytes_backward":    "init_bwd_win_byts",
    "act_data_pkt_fwd":           "act_data_pkt_fwd",
    "min_seg_size_forward":       "min_seg_size_forward",
    "Active Mean":                "active_mean",
    "Active Std":                 "active_std",
    "Active Max":                 "active_max",
    "Active Min":                 "active_min",
    "Idle Mean":                  "idle_mean",
    "Idle Std":                   "idle_std",
    "Idle Max":                   "idle_max",
    "Idle Min":                   "idle_min",
}


def _ip_to_int(ip: str) -> int:
    try:
        val = struct.unpack("!I", socket.inet_aton(ip))[0]
        # Convert unsigned 32-bit to signed 32-bit (C# System.Int32)
        return val if val <= 0x7FFFFFFF else val - 0x100000000
    except OSError:
        return 0


def to_feature_vector(flow: dict) -> list[float]:
    """
    Extract a numeric feature vector in FEATURE_NAMES order.
    Missing keys default to 0.0.
    The result is passed to pipeline/normalizer → model/predictor.
    """
    return [float(flow.get(_CIC_KEY[name], 0)) for name in FEATURE_NAMES]


def to_connection_payload(flow: dict) -> tuple["ConnectionPayload", list[float]]:
    """
    Convert a cicflowmeter flow dict into:
      - ConnectionPayload for ASP.NET (metadata + traits as JSON feature vector)
      - numeric feature vector for the model

    Returns both so the caller (pipeline) doesn't extract features twice.
    """
    features = to_feature_vector(flow)
    traits = json.dumps(dict(zip(FEATURE_NAMES, features)))

    payload = ConnectionPayload(
        timestamp=datetime.now(timezone.utc),
        srcIP=_ip_to_int(flow.get("src_ip", "0.0.0.0")),
        dstIP=_ip_to_int(flow.get("dst_ip", "0.0.0.0")),
        srcPort=int(flow.get("src_port", 0)),
        dstPort=int(flow.get("dst_port", 0)),
        protocol=str(flow.get("protocol", "")),
        service=str(flow.get("dst_port", "")),
        duration=float(flow.get(_CIC_KEY["Flow Duration"], 0)),
        srcBytes=int(flow.get(_CIC_KEY["Total Length of Fwd Packets"], 0)),
        dstBytes=int(flow.get(_CIC_KEY["Total Length of Bwd Packets"], 0)),
        traits=traits,
    )
    return payload, features
