"""One line per background job: python -m experiments.status"""
from __future__ import annotations

import json
import os
import sys
from pathlib import Path

from promptevo.config import resolve


def _alive(pid: int) -> bool:
    if os.name == "nt":          # signal 0 is CTRL_C_EVENT on Windows: os.kill can't probe
        import ctypes
        k32 = ctypes.windll.kernel32
        h = k32.OpenProcess(0x1000, False, pid)      # PROCESS_QUERY_LIMITED_INFORMATION
        if not h:
            return False
        code = ctypes.c_ulong()
        ok = k32.GetExitCodeProcess(h, ctypes.byref(code))
        k32.CloseHandle(h)
        return bool(ok) and code.value == 259          # STILL_ACTIVE
    try:
        os.kill(pid, 0)
        return True
    except OSError:
        return False


def main() -> None:
    logs = resolve(sys.argv[1] if len(sys.argv) > 1 else "logs")
    files = sorted(logs.glob("*.progress.json"))
    if not files:
        print("no jobs (no logs/*.progress.json)")
        return
    for f in files:
        r = json.loads(f.read_text())
        state = r.get("state")
        if state == "running" and not _alive(int(r.get("pid", -1))):
            state = "DEAD?"
        tot = r.get("total") or "?"
        eta = r.get("eta_s")
        eta_s = f"eta {eta/60:.0f}m" if isinstance(eta, (int, float)) else ""
        print(f"{r['job']:<28} {state:<8} {r.get('done')}/{tot} {eta_s:<10} upd {r.get('updated')}"
              + (f" err={r['error'][:60]}" if r.get("error") else ""))


if __name__ == "__main__":
    main()
