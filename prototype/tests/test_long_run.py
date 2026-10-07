"""Long runs: stop file, clean stop on errors, resume by replay, run_info, progress time box."""
import argparse
import json
import re
import time

from experiments.smoke_run import run, write_info
from promptevo.backends.factory import make_rewriter
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.cache import KVCache
from promptevo.config import load_config
from promptevo.llm.ollama_client import OllamaClient
from promptevo.progress import Progress, keep_awake
from promptevo.sim import Simulation

MUTATING = {"evolution": {"p_mut": 0.3}, "ollama": {"mutator_model": "m"}}


def fake_mutator(calls):
    """Ollama stand-in: a fixed answer per (gene, seed); records every call that reaches it."""
    def transport(method, path, payload):
        if path == "/api/tags":
            return {"models": [{"name": "m", "digest": "d" * 12}]}
        calls.append(payload)
        gene = re.search(r'"(.*)"', payload["messages"][0]["content"]).group(1)
        return {"message": {"content": f"{gene.split()[0]} quickly {payload['options']['seed'] % 89}."}}
    return transport


def make_sim(cfg, cache_path, out, calls, transport=None):
    client = OllamaClient(cache=KVCache(cache_path), transport=transport or fake_mutator(calls), retries=1)
    rewriter, model = make_rewriter(cfg, client)
    return Simulation(cfg, RuleBasedBackend(), seed=5, out_dir=out, rewriter=rewriter, rewriter_model=model)


def test_stop_file_then_resume_by_replay(tmp_path):
    cfg = load_config("small", MUTATING)
    ref_calls, first_calls, again_calls = [], [], []
    ref = make_sim(cfg, tmp_path / "ref.sqlite", tmp_path / "ref", ref_calls)
    assert run(ref, 1000) == "ticks"
    ref.finish()
    first = make_sim(cfg, tmp_path / "c.sqlite", tmp_path / "run", first_calls)
    stop = tmp_path / "run.stop"
    assert run(first, 650, stop_file=stop) == "ticks"
    stop.touch()                                     # asked to stop: it does at the next look (tick 700)
    assert run(first, 1000, stop_file=stop) == "stop file" and first.t == 700 and not stop.exists()
    first.finish()
    assert first_calls                               # the stopped part did ask the model
    again = make_sim(cfg, tmp_path / "c.sqlite", tmp_path / "run", again_calls)
    assert run(again, 700) == "ticks" and not again_calls          # replayed from the cache: no calls
    assert run(again, 1000) == "ticks"
    again.finish()
    assert len(first_calls) + len(again_calls) == len(ref_calls)    # nothing asked twice
    assert (tmp_path / "run" / "events.jsonl").read_text() == (tmp_path / "ref" / "events.jsonl").read_text()


def test_model_failure_stops_cleanly(tmp_path, monkeypatch):
    monkeypatch.setattr("promptevo.llm.ollama_client.time.sleep", lambda s: None)
    cfg = load_config("small", MUTATING)

    def down(method, path, payload):
        if path == "/api/tags":
            return {"models": []}
        raise ConnectionRefusedError("no server")
    sim = make_sim(cfg, tmp_path / "c.sqlite", tmp_path / "run", [], transport=down)
    why = run(sim, 3000)
    assert why.startswith("error: RuntimeError") and 0 < sim.t < 3000
    sim.finish()
    for f in ("summary.json", "final_population.json", "alleles.jsonl"):
        assert (tmp_path / "run" / f).exists()


def test_run_info_and_progress_time_box(tmp_path):
    a = argparse.Namespace(profile="small", world=None, ticks=10, minutes=None)
    write_info(tmp_path, a, load_config("small"), "rule_based", 3, None)
    info = json.loads((tmp_path / "run_info.json").read_text(encoding="utf-8"))
    assert info["seed"] == 3 and info["git"]["commit"] and "ollama" not in info
    assert info["config"]["evolution"]["mutation_prompts"] == "prompts/mutate_v3.txt"
    with keep_awake():
        p = Progress(tmp_path, "job", total=10**6, deadline=time.time() + 60)
        p.update(10)
    r = json.loads((tmp_path / "job.progress.json").read_text())
    assert r["eta_s"] <= 60 and r["ends"]            # a time-boxed job ends by its deadline
    p.stop(20, "stop file")
    r = json.loads((tmp_path / "job.progress.json").read_text())
    assert r["state"] == "stopped" and r["reason"] == "stop file" and r["eta_s"] is None
