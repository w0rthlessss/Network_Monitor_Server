import asyncio
from datetime import datetime, timezone
from typing import Awaitable, Callable

import psutil

from schemas import SystemUsagePayload

_MB = 1024 ** 2


async def run(
    send: Callable[[SystemUsagePayload], Awaitable[None]],
    interval: int = 5,
) -> None:
    """
    Periodically samples system metrics and calls *send* with the result.
    Meant to run as a long-lived asyncio background task.

    Network usage is the total GB transferred (in + out) during the interval.
    CPU and RAM are percentages in [0, 100].
    """
    prev_net = psutil.net_io_counters()
    psutil.cpu_percent()  # prime the counter — first call always returns 0.0

    while True:
        await asyncio.sleep(interval)

        cur_net = psutil.net_io_counters()
        net_mb = (
            (cur_net.bytes_sent + cur_net.bytes_recv)
            - (prev_net.bytes_sent + prev_net.bytes_recv)
        ) / _MB
        prev_net = cur_net

        try:
            active_connections = len(psutil.net_connections(kind="inet"))
        except psutil.AccessDenied:
            active_connections = 0

        payload = SystemUsagePayload(
            timestamp=datetime.now(timezone.utc),
            cpuUsage=psutil.cpu_percent(),
            memoryUsage=psutil.virtual_memory().percent,
            networkUsage=round(net_mb, 3),
            activeConnections=active_connections,
        )

        try:
            await send(payload)
        except Exception as exc:
            print(f"[system_usage] send failed: {exc!r}")
