import json
import re

import numpy as np

from experiments.label_teacher import check, done_keys, label
from experiments.make_dataset import build
from experiments.make_mutants import make_mutants
from experiments.make_obs import synthetic
from experiments.teacher_gate import run_gate
from promptevo.backends.ollama_policy import TeacherBackend
from promptevo.config import resolve
from promptevo.genome import ACTIONS, LOCI
from promptevo.llm.ollama_client import OllamaClient


def fake_transport(calls):
    """Teacher that reads the genes: an action whose gene says 'always' gets most points."""
    def t(method, path, payload):
        if path == "/api/tags":
            return {"models": [{"name": "teach:latest", "digest": "d" * 12}]}
        calls.append(1)
        prompt = payload["messages"][0]["content"]
        genes = dict(re.findall(r'- (\w+): "([^"]*)"', prompt))
        pts = {a: (80 if "always" in genes.get(a, "").lower() else
                   1 if "never" in genes.get(a, "").lower() else 10) for a in ACTIONS}
        if "action" in payload["format"]["properties"]:
            content = json.dumps({"action": max(pts, key=pts.get)})
        else:
            content = json.dumps(pts)
        return {"message": {"content": content}}
    return t


def fake_mutator(prompt, seed):
    """Stands in for the mutator LLM: appends one of six endings, chosen by the seed."""
    text = re.search(r'"(.*)"', prompt).group(1).rstrip(".")
    return f"{text} {['slowly', 'at night', 'near others', 'when calm', 'quietly', 'alone'][seed % 6]}."


def _dataset(cfg, reg_pools, n=240):
    reg, pools = reg_pools
    mutants = make_mutants(cfg, reg, pools, np.random.default_rng(1), per_allele=3, llm=fake_mutator, model="fake")
    rows, meta = build(cfg, reg, pools, mutants, synthetic(), n, seed=5)
    return rows, meta


def test_dataset_splits_hold_out_alleles(cfg, reg_pools):
    rows, meta = _dataset(cfg, reg_pools)
    assert {r["split"] for r in rows} == {"train", "val", "test"}
    train_texts = {(l, r["genes"][l]) for r in rows if r["split"] == "train" for l in LOCI}
    for r in rows:
        if r["split"] != "train":
            for l in r["held_out_loci"]:
                assert (l, r["genes"][l]) not in train_texts          # unseen in training
    assert len({r["key"] for r in rows}) == len(rows)
    # contrast groups: same observation, genomes differ at exactly the varied locus
    groups = {}
    for r in rows:
        if r["kind"] == "contrast":
            groups.setdefault((r["group"], json.dumps(r["obs"], sort_keys=True)), []).append(r)
    assert groups
    for g in groups.values():
        loc = g[0]["varied_locus"]
        for a in g[1:]:
            diff = [l for l in LOCI if a["genes"][l] != g[0]["genes"][l]]
            assert diff == [loc]
    contrast = {c for r in rows for c in r["genes"].values()}
    reg, pools = reg_pools
    for pro, anti in pools.contrast.values():
        assert reg.text(pro) not in contrast and reg.text(anti) not in contrast


def test_label_resume_and_check(cfg, reg_pools, tmp_path):
    rows, _ = _dataset(cfg, reg_pools, n=120)
    calls = []
    client = OllamaClient(transport=fake_transport(calls))
    teacher = TeacherBackend(client, "teach", resolve("prompts/teacher_v2.md"), strict=True)
    out = tmp_path / "labels.jsonl"
    r1 = label(rows[:50], teacher, out, {"model": "teach"}, workers=2)
    assert r1["written"] == 50 and r1["failed"] == 0
    n_calls = len(calls)
    r2 = label(rows, teacher, out, {"model": "teach"}, workers=2)
    assert r2["already"] == 50 and r2["written"] == len(rows) - 50
    assert len(calls) == n_calls + len(rows) - 50                    # no re-labelling
    assert len(done_keys(out)) == len(rows)
    rec = json.loads(out.read_text().splitlines()[0])
    assert abs(sum(rec["probs"]) - 1) < 1e-3 and len(rec["probs"]) == len(ACTIONS)
    lines = check(rows, out)
    assert lines[0].endswith(f"{len(rows)}/{len(rows)} rows labelled")


def test_label_skips_failures(cfg, reg_pools, tmp_path):
    rows, _ = _dataset(cfg, reg_pools, n=60)

    def broken(method, path, payload):
        if path == "/api/tags":
            return {"models": []}
        raise RuntimeError("model not loaded")
    teacher = TeacherBackend(OllamaClient(transport=broken, retries=1), "x",
                             resolve("prompts/teacher_v2.md"), strict=True)
    r = label(rows[:5], teacher, tmp_path / "l.jsonl", {}, workers=1)
    assert r["written"] == 0 and r["failed"] == 5


def test_teacher_gate_with_fake(cfg):
    calls = []
    client = OllamaClient(transport=fake_transport(calls))
    results, agreement = run_gate(cfg, client, ["points", "ksample"], 12, 1,
                                  resolve("prompts/teacher_v2.md"), "teach", logs=False)
    assert results["points"]["sign_acc"] > 0.9 and results["points"]["gate"]["directed"]
    assert agreement is not None and agreement >= 0
