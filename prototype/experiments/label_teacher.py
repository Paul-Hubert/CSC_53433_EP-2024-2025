"""S4.6 — label the dataset with the local teacher LLM (resumable, background-friendly).

    nohup python -m experiments.label_teacher --splits val,test,train --workers 4 \
        > logs/label_teacher.log 2>&1 &
    python -m experiments.status                      # progress / ETA
    python -m experiments.label_teacher --check       # sanity stats, no model calls

Output: data/labels_v1_<model>_<mode>.jsonl, one line per dataset key:
  {key, split, probs[7], model, digest, mode, prompt, style}
Re-running skips keys already labelled. Failed calls are not written (retried next run).
Tip (GPU): OLLAMA_NUM_PARALLEL=4 on the server + --workers 4. Unload the teacher
(`ollama stop <model>`) before fine-tuning Laya on the same GPU.
"""
from __future__ import annotations

import argparse
import json
import re
from concurrent.futures import ThreadPoolExecutor
from itertools import combinations
from pathlib import Path

import numpy as np

from experiments.make_dataset import load_rows
from experiments.make_obs import obs_from_dict
from promptevo import metrics as M
from promptevo.backends.base import Query
from promptevo.cache import make_key
from promptevo.config import load_config, resolve
from promptevo.genome import ACTIONS, LOCI
from promptevo.progress import Progress


def labels_path(cfg, model: str, mode: str, version: str = "v1") -> Path:
    slug = re.sub(r"[^A-Za-z0-9._-]+", "-", model)
    return resolve(cfg.paths.data_dir) / f"labels_{version}_{slug}_{mode}.jsonl"


def row_query(row: dict) -> Query:
    genes = row["genes"]
    return Query(make_key("genes", [genes[l] for l in LOCI])[:24], genes, obs_from_dict(row["obs"]))


def done_keys(path: Path) -> set[str]:
    if not path.exists():
        return set()
    keys = set()
    for line in path.read_text().splitlines():
        try:
            keys.add(json.loads(line)["key"])
        except (json.JSONDecodeError, KeyError):
            continue                                   # tolerate a torn last line
    return keys


def label(rows: list[dict], teacher, out: Path, meta: dict, workers: int = 1,
          prog: Progress | None = None, batch: int = 16) -> dict:
    """Label rows not yet in `out`. teacher must have strict=True. Returns counts."""
    done = done_keys(out)
    todo = [r for r in rows if r["key"] not in done]
    written = failed = 0

    def one(r):
        try:
            return r, teacher._one(row_query(r))
        except Exception as e:                        # skip; retried on the next run
            return r, e

    with out.open("a") as f, ThreadPoolExecutor(max(1, workers)) as ex:
        for s in range(0, len(todo), batch):
            for r, p in ex.map(one, todo[s:s + batch]):
                if isinstance(p, Exception):
                    failed += 1
                    continue
                f.write(json.dumps({"key": r["key"], "split": r["split"],
                                    "probs": [round(float(x), 5) for x in p], **meta}) + "\n")
                written += 1
            f.flush()
            if prog:
                prog.update(len(done) + written, failed=failed)
    return {"already": len(done), "written": written, "failed": failed, "todo": len(todo)}


def check(rows: list[dict], path: Path) -> list[str]:
    """Sanity stats: entropy, near-uniform share, contrast-group spread vs random pairs."""
    labels = {}
    for line in path.read_text().splitlines():
        try:
            d = json.loads(line)
            labels[d["key"]] = np.array(d["probs"])
        except (json.JSONDecodeError, KeyError):
            continue
    have = [r for r in rows if r["key"] in labels]
    if not have:
        return [f"{path.name}: no labels yet"]
    P = np.array([labels[r["key"]] for r in have])
    H = M.entropy(P)
    hmax = np.log2(len(ACTIONS))
    lines = [f"{path.name}: {len(have)}/{len(rows)} rows labelled",
             f"mean entropy {H.mean():.2f} bits (max {hmax:.2f}); near-uniform (>95% max) {np.mean(H > .95 * hmax):.1%}",
             "mean probs: " + " ".join(f"{a}={v:.2f}" for a, v in zip(ACTIONS, P.mean(0))),
             "top action share: " + " ".join(f"{a}={v:.2f}" for a, v in
                                             zip(ACTIONS, np.bincount(P.argmax(1), minlength=len(ACTIONS)) / len(P)))]
    groups = {}
    for r in have:
        if r["kind"] == "contrast":
            groups.setdefault((r["group"], json.dumps(r["obs"], sort_keys=True)), []).append(labels[r["key"]])
    within = [float(M.jsd(a, b)) for g in groups.values() for a, b in combinations(g, 2)]
    rng = np.random.default_rng(0)
    idx = rng.integers(len(P), size=(min(2000, len(P) * 2), 2))
    between = [float(M.jsd(P[i], P[j])) for i, j in idx if i != j]
    if within:
        lines.append(f"contrast groups: mean JSD between gene variants {np.mean(within):.3f} "
                     f"(random pairs {np.mean(between):.3f}); share of variant pairs with JSD>0.05: "
                     f"{np.mean(np.array(within) > .05):.1%}")
        lines.append("→ genes matter to the teacher if variant JSD is clearly > 0 (ideally ≥ 1/3 of random pairs).")
    return lines


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--splits", default="val,test,train")
    ap.add_argument("--model", default=None)
    ap.add_argument("--mode", default=None, help="points | ksample (default: cfg.ollama.teacher_mode)")
    ap.add_argument("--prompt", default="prompts/teacher_v4.md")
    ap.add_argument("--workers", type=int, default=1)
    ap.add_argument("--limit", type=int, default=None, help="label at most N new rows (e.g. 50 for a first look)")
    ap.add_argument("--check", action="store_true", help="only print sanity stats of existing labels")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    model = a.model or cfg.ollama.teacher_model
    mode = a.mode or cfg.ollama.teacher_mode
    if not model:
        raise SystemExit("set ollama.teacher_model in configs/base.yaml or pass --model")
    rows = [r for s in a.splits.split(",") for r in load_rows(s, cfg.paths.data_dir)]
    out = labels_path(cfg, model, mode)
    if a.check:
        print("\n".join(check(rows, out) if out.exists() else [f"{out} does not exist"]))
        return
    from promptevo.backends.ollama_policy import TeacherBackend
    from promptevo.llm.ollama_client import client_from_config
    client = client_from_config(cfg)
    teacher = TeacherBackend(client, model, resolve(a.prompt), mode=mode, style=cfg.backend.obs_style,
                             k=int(cfg.ollama.ksample_k), strict=True)
    meta = {"model": model, "digest": client.digest(model), "mode": mode,
            "prompt": Path(a.prompt).name, "style": cfg.backend.obs_style}
    if a.limit is not None:
        done = done_keys(out)
        rows = [r for r in rows if r["key"] in done] + [r for r in rows if r["key"] not in done][: a.limit]
    prog = Progress(resolve(cfg.paths.logs_dir), "label_teacher", total=len(rows))
    try:
        res = label(rows, teacher, out, meta, a.workers, prog)
    except KeyboardInterrupt:
        prog.update(len(done_keys(out)), state="stopped")
        raise
    prog.finish(res["already"] + res["written"], failed=res["failed"])
    print(json.dumps(res))
    print("\n".join(check(rows, out)))


if __name__ == "__main__":
    main()
