"""Progress files for background jobs: logs/<job>.progress.json (atomic writes)."""
from __future__ import annotations

import json
import os
import time
from pathlib import Path


class Progress:
    def __init__(self, logs_dir: str | Path, job: str, total: int | None = None):
        self.path = Path(logs_dir) / f"{job}.progress.json"
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.job, self.total, self.started = job, total, time.time()
        self.update(done=0, state="running")

    def update(self, done: int, state: str = "running", **extra) -> None:
        elapsed = time.time() - self.started
        eta = None
        if self.total and done:
            eta = elapsed / done * (self.total - done)
        rec = {"job": self.job, "pid": os.getpid(), "state": state, "done": done,
               "total": self.total, "elapsed_s": round(elapsed, 1),
               "eta_s": None if eta is None else round(eta, 1),
               "updated": time.strftime("%Y-%m-%d %H:%M:%S"), **extra}
        tmp = self.path.with_suffix(".tmp")
        tmp.write_text(json.dumps(rec))
        os.replace(tmp, self.path)

    def finish(self, done: int, **extra) -> None:
        self.update(done, state="done", **extra)

    def fail(self, done: int, error: str) -> None:
        self.update(done, state="failed", error=error[:300])
