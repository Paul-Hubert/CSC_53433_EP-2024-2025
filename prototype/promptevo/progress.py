"""Progress files for background jobs: logs/<job>.progress.json (atomic writes)."""
from __future__ import annotations

import json
import os
import time
from contextlib import contextmanager
from pathlib import Path

from .eventlog import replace_file


class Progress:
    def __init__(self, logs_dir: str | Path, job: str, total: int | None = None,
                 deadline: float | None = None):
        self.path = Path(logs_dir) / f"{job}.progress.json"
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.job, self.total, self.started = job, total, time.time()
        self.deadline = deadline           # epoch seconds: a time-boxed job ends by then
        self.update(done=0, state="running")

    def update(self, done: int, state: str = "running", **extra) -> None:
        now = time.time()
        elapsed = now - self.started
        eta = None
        if state == "running":
            if self.total and done:
                eta = elapsed / done * (self.total - done)
            if self.deadline:
                left = max(0.0, self.deadline - now)
                eta = left if eta is None else min(eta, left)
        rec = {"job": self.job, "pid": os.getpid(), "state": state, "done": done,
               "total": self.total, "elapsed_s": round(elapsed, 1),
               "eta_s": None if eta is None else round(eta, 1),
               **({"ends": time.strftime("%Y-%m-%d %H:%M", time.localtime(self.deadline))} if self.deadline else {}),
               "updated": time.strftime("%Y-%m-%d %H:%M:%S"), **extra}
        tmp = self.path.with_suffix(".tmp")
        tmp.write_text(json.dumps(rec))
        replace_file(tmp, self.path)       # status readers may hold the file for a moment

    def finish(self, done: int, **extra) -> None:
        self.update(done, state="done", **extra)

    def stop(self, done: int, reason: str) -> None:
        self.update(done, state="stopped", reason=reason)

    def fail(self, done: int, error: str) -> None:
        self.update(done, state="failed", error=error[:300])


@contextmanager
def keep_awake():
    """Ask Windows not to sleep while a long job runs. Changes no power setting and ends with the
    job; elsewhere it does nothing."""
    k32 = None
    if os.name == "nt":
        try:
            import ctypes
            k32 = ctypes.windll.kernel32
            k32.SetThreadExecutionState(0x80000000 | 0x00000001)    # ES_CONTINUOUS | ES_SYSTEM_REQUIRED
        except (AttributeError, OSError):
            k32 = None
    try:
        yield
    finally:
        if k32 is not None:
            k32.SetThreadExecutionState(0x80000000)                  # ES_CONTINUOUS alone: back to normal
