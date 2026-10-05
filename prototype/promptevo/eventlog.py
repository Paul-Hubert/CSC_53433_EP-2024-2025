"""JSONL event log with a running hash (determinism checks) + stats CSV."""
from __future__ import annotations

import csv
import hashlib
import json
import os
import time
from pathlib import Path


def replace_file(tmp: Path, path: Path, tries: int = 5) -> bool:
    """os.replace that waits out a reader holding `path` open (Windows refuses the rename
    meanwhile). False if it never could; the caller tries again at its next save."""
    for i in range(tries):
        try:
            os.replace(tmp, path)
            return True
        except PermissionError:
            time.sleep(0.2 * (i + 1))
    return False


class EventLog:
    def __init__(self, out_dir: str | Path | None):
        self.sha = hashlib.sha256()
        self.count = 0
        self.out_dir = Path(out_dir) if out_dir else None
        self._f = self._stats_f = self._stats_w = None
        if self.out_dir:
            self.out_dir.mkdir(parents=True, exist_ok=True)
            self._f = (self.out_dir / "events.jsonl").open("w")

    def event(self, kind: str, t: int, **fields) -> None:
        line = json.dumps({"kind": kind, "t": t, **fields}, ensure_ascii=False, sort_keys=True)
        self.sha.update(line.encode())
        self.count += 1
        if self._f:
            self._f.write(line + "\n")

    def stats(self, row: dict) -> None:
        if not self.out_dir:
            return
        if self._stats_w is None:
            self._stats_f = (self.out_dir / "stats.csv").open("w", newline="")
            self._stats_w = csv.DictWriter(self._stats_f, fieldnames=list(row))
            self._stats_w.writeheader()
        self._stats_w.writerow(row)

    def flush(self, sync: bool = False) -> None:
        """Push buffered lines to the OS (and to disk with sync=True): a hard stop then loses only
        what came after."""
        for f in (self._f, self._stats_f):
            if f:
                f.flush()
                if sync:
                    os.fsync(f.fileno())

    def digest(self) -> str:
        return self.sha.hexdigest()

    def close(self) -> None:
        for f in (self._f, self._stats_f):
            if f:
                f.close()
