from datetime import datetime
from typing import Optional
from pydantic import BaseModel


# ---------------------------------------------------------------------------
# ASP.NET → Python
# ---------------------------------------------------------------------------

class SetActiveModelRequest(BaseModel):
    modelId: int
    modelPath: str


class HyperparametersPayload(BaseModel):
    epochs: int = 50
    batchSize: int = 512
    hiddenSize: int = 128
    dropout: float = 0.4
    learningRate: float = 1e-3
    weightDecay: float = 0.0
    earlyStoppingPatience: int = 5
    posWeightFactor: float = 1.0
    targetRecall: float = 0.0
    outputName: str = "model"


class TrainRequest(BaseModel):
    modelId: Optional[int] = None
    userId: int
    hyperparameters: HyperparametersPayload = HyperparametersPayload()


class ActiveModelResponse(BaseModel):
    id: int
    modelPath: str


# ---------------------------------------------------------------------------
# Python → ASP.NET  (POST /internal/traffic)
# ---------------------------------------------------------------------------

class ConnectionPayload(BaseModel):
    timestamp: datetime
    srcIP: int
    dstIP: int
    srcPort: int
    dstPort: int
    protocol: str
    service: str
    duration: float
    srcBytes: int
    dstBytes: int
    traits: str


class PredictionPayload(BaseModel):
    modelId: int
    result: bool
    confidence: float
    topFeature: str
    timestamp: datetime


class TrafficRecordPayload(BaseModel):
    connection: ConnectionPayload
    prediction: PredictionPayload
    alertDescription: Optional[str] = None


# ---------------------------------------------------------------------------
# Python → ASP.NET  (POST /internal/system-usage)
# ---------------------------------------------------------------------------

class SystemUsagePayload(BaseModel):
    timestamp: datetime
    cpuUsage: float
    memoryUsage: float
    networkUsage: float
    activeConnections: int


# ---------------------------------------------------------------------------
# Python → ASP.NET  (POST /internal/models)
# ---------------------------------------------------------------------------

class CreateModelPayload(BaseModel):
    userId: int
    metrics: str
    modelPath: str


# ---------------------------------------------------------------------------
# Python → ASP.NET  (PATCH /internal/models/{id})
# ---------------------------------------------------------------------------

class UpdateModelPayload(BaseModel):
    metrics: str
    modelPath: str
