import os
from pathlib import Path

from dotenv import load_dotenv

# В Docker переменные приходят из docker-compose; локально читаем .env из корня проекта
_env_file = Path(__file__).parent.parent / ".env"
load_dotenv(_env_file, override=False)

ASPNET_BASE_URL: str = os.getenv("ASPNET_BASE_URL", "http://localhost:8080")
INTERNAL_API_KEY: str = os.getenv("INTERNAL_API_KEY", "")
SYSTEM_USAGE_INTERVAL: int = int(os.getenv("SYSTEM_USAGE_INTERVAL", "5"))
DATA_SOURCE: str = os.getenv("DATA_SOURCE", "")
DEFAULT_USER_ID: int = int(os.getenv("DEFAULT_USER_ID", "1"))
THRESHOLD_OVERRIDE: float = float(os.getenv("THRESHOLD_OVERRIDE", "0"))
BYPASS_MODEL: bool = os.getenv("BYPASS_MODEL", "false").lower() == "true"
