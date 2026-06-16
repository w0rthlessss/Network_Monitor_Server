import asyncio
import platform
import threading
from typing import Callable

from scapy.all import sniff
from scapy.sessions import DefaultSession
import cicflowmeter.flow_session as _cflow
from cicflowmeter.flow_session import FlowSession
from cicflowmeter.utils import get_logger

FLOW_TIMEOUT = 15

# Override cicflowmeter's hardcoded constants
_cflow.EXPIRED_UPDATE = FLOW_TIMEOUT
_cflow.PACKETS_PER_GC = 50


def _resolve_iface(name: str) -> str:
    if platform.system() != "Windows":
        return name
    if name.startswith(r"\Device\NPF"):
        return name
    try:
        from scapy.arch.windows import get_windows_if_list
        for iface in get_windows_if_list():
            if iface.get("name") == name or iface.get("description") == name:
                guid = iface.get("guid", "")
                if guid:
                    return rf"\Device\NPF_{guid}"
    except Exception:
        pass
    return name


class _CallbackWriter:
    def __init__(self, callback: Callable[[dict], None]) -> None:
        self._callback = callback

    def write(self, data: dict) -> None:
        self._callback(data)


def _make_session_class(on_flow_cb: Callable[[dict], None]) -> type:
    writer = _CallbackWriter(on_flow_cb)

    class _Session(FlowSession):
        def __init__(self, *args, **kwargs):
            self.flows = {}
            self.verbose = False
            self.fields = None
            self.output_mode = None
            self.output = None
            self.logger = get_logger(False)
            self.packets_count = 0
            self.output_writer = writer
            self._lock = threading.Lock()
            DefaultSession.__init__(self, *args, **kwargs)

    return _Session


async def run_live(
    interface: str,
    on_flow: Callable[[dict], None],
    flow_timeout: int = FLOW_TIMEOUT,
) -> None:
    loop = asyncio.get_running_loop()
    stop_event = threading.Event()

    def _on_flow_sync(flow_dict: dict) -> None:
        loop.call_soon_threadsafe(on_flow, flow_dict)

    SessionClass = _make_session_class(_on_flow_sync)
    resolved = _resolve_iface(interface)
    print(f"[sniffer] capturing on: {resolved}")

    def _capture() -> None:
        sniff(
            iface=resolved,
            session=SessionClass,
            store=False,
            stop_filter=lambda _: stop_event.is_set(),
        )

    try:
        await asyncio.to_thread(_capture)
    except asyncio.CancelledError:
        stop_event.set()
        raise


async def run_pcap(
    pcap_path: str,
    on_flow: Callable[[dict], None],
) -> None:
    flows: list[dict] = []

    def _collect(flow_dict: dict) -> None:
        flows.append(flow_dict)

    SessionClass = _make_session_class(_collect)
    await asyncio.to_thread(sniff, offline=pcap_path, session=SessionClass, store=False)

    for flow in flows:
        on_flow(flow)
