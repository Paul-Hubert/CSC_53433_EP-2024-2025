"""Look at a data file without flooding the context: python -m experiments.peek FILE [-n 5]"""
from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("file")
    ap.add_argument("-n", type=int, default=5)
    ap.add_argument("--width", type=int, default=200)
    a = ap.parse_args()
    p = Path(a.file)
    if p.suffix == ".csv":
        with p.open() as f:
            rows = list(csv.reader(f))
        print(f"{p} — {len(rows) - 1} rows; columns: {rows[0] if rows else []}")
        for r in rows[1:a.n + 1]:
            print(",".join(r)[:a.width])
        if len(rows) > a.n + 1:
            print("last:", ",".join(rows[-1])[:a.width])
    elif p.suffix == ".jsonl":
        lines = p.read_text().splitlines()
        print(f"{p} — {len(lines)} lines")
        keys = sorted(json.loads(lines[0]).keys()) if lines else []
        print("keys:", keys)
        for ln in lines[:a.n]:
            print(ln[:a.width])
    else:
        text = p.read_text()
        print(f"{p} — {len(text)} chars")
        print(text[:a.width * a.n])


if __name__ == "__main__":
    main()
