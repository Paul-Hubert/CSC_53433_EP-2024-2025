import json

import numpy as np
import pytest

from promptevo.backends.base import Query
from promptevo.backends.laya_backend import LayaBackend, build_request, extract_probs
from promptevo.backends.ollama_policy import TeacherBackend
from promptevo.config import resolve
from promptevo.genome import ACTIONS
from promptevo.llm.ollama_client import OllamaClient, make_rewriter
from promptevo.perception import Observation

O = Observation("low", "close", "far", "none")


def test_build_request_placements(reg_pools):
    reg, pools = reg_pools
    genes = reg.genes(pools.neutral_genome())
    for p in ("P1", "P2", "P3", "P4"):
        state, q = build_request(genes, "Energy: low.", p)
        assert set(q["action"]["criteria"]) == set(ACTIONS) and "situation" in state
    assert "Instincts" in build_request(genes, "x", "P2")[0]["animal"]


def test_extract_probs_variants():
    p, exact = extract_probs({"probabilities": {a: 1.0 for a in ACTIONS}})
    assert exact and np.allclose(p, 1 / len(ACTIONS))
    p, exact = extract_probs({"choice": "flee", "confidence": 0.9})
    assert not exact and p.argmax() == ACTIONS.index("flee")
    with pytest.raises(ValueError):
        extract_probs({"nothing": 1})


class FakeLaya:
    id = "fake"

    def __init__(self):
        self.calls = 0

    def predict(self, state, questions):
        self.calls += 1
        crit = questions["action"]["criteria"]
        scores = {a: 2.0 if "Always" in crit[a] else 1.0 for a in ACTIONS}
        return {"answers": {"action": {"choice": max(scores, key=scores.get), "probabilities": scores}}}


def test_laya_backend_with_fake_and_cache(reg_pools):
    reg, pools = reg_pools
    fake = FakeLaya()
    b = LayaBackend(fake, "P4", "V1")
    pro, _ = pools.contrast_pair("flee")
    q = Query(reg.genome_key(pro), reg.genes(pro), O)
    p1 = b.decide([q, q])
    assert fake.calls == 1 and p1[0].argmax() == ACTIONS.index("flee")


def fake_transport(method, path, payload):
    if path == "/api/tags":
        return {"models": [{"name": "teach:latest", "digest": "abc123def456aa"}]}
    if path == "/api/version":
        return {"version": "fake"}
    if path == "/api/chat":
        schema = payload.get("format")
        if schema and "action" in schema["properties"]:
            content = json.dumps({"action": "flee"})
        elif schema:
            content = json.dumps({a: (70 if a == "flee" else 5) for a in ACTIONS})
        else:
            content = "Run from every shadow."
        return {"message": {"content": content}, "eval_count": 10, "eval_duration": 1e9}
    raise AssertionError(path)


def test_ollama_client_and_teacher(reg_pools):
    reg, pools = reg_pools
    c = OllamaClient(transport=fake_transport)
    assert c.digest("teach") == "abc123def456"
    g = pools.neutral_genome()
    q = Query(reg.genome_key(g), reg.genes(g), O)
    for mode in ("points", "ksample"):
        t = TeacherBackend(c, "teach", resolve("prompts/teacher_v3.md"), mode=mode, k=3)
        p = t.decide([q])
        assert p[0].argmax() == ACTIONS.index("flee") and abs(p.sum() - 1) < 1e-9
    ask = make_rewriter(c, "teach", temperature=1.2)
    assert ask('Make one random change to this sentence.\n\n"Eat."', 1) == "Run from every shadow."
