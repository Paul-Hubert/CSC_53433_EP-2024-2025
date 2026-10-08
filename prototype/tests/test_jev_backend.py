import math

import numpy as np
import pytest

from promptevo.backends.base import Query
from promptevo.backends.factory import make_backend, make_rewriter, mutator_client
from promptevo.backends.jev_backend import JevBackend, JevModel, jev_text, option_probs
from promptevo.cache import KVCache
from promptevo.config import load_config, resolve
from promptevo.genome import ACTIONS
from promptevo.llm.openai_client import OpenAIClient
from promptevo.perception import Observation
from promptevo.species import PREDATOR

OBS = [Observation("low", f, p, "none") for f in ("none", "close") for p in ("none", "close")]
GENES = {a: f"Always {a} when you can." if a == "flee" else f"Sometimes {a}." for a in ACTIONS}
IDS = [3721, 1802, 15, 16, 17, 18, 19, 20] + list(range(32, 48))          # judge_config.json
HEAD = {"bias": [0.0] * 8 + [0.5] + [0.0] * 15, "verbalizer_ids": IDS,
        "slots": {"ranges": {"noul": [0, 2], "score": [2, 8], "choice": [8, 24]}}}


def options_of(text):
    return [l.split(") ", 1)[1] for l in text.split("[options]\n")[1].splitlines() if ") " in l]


def fake_vllm(log):
    """/v1/completions like vLLM with allowed_token_ids: log-probs favour the 'Always' gene's letter."""
    def t(method, path, payload):
        if path == "/v1/models":
            return {"data": [{"id": "jev-decision", "root": "/models/JEV-9B/adapter_vllm"},
                             {"id": "mut", "root": "Qwen/Qwen3.5-0.8B"}]}
        if path == "/version":
            return {"version": "0.test"}
        log.append((path, payload))
        if path == "/v1/chat/completions":
            return {"choices": [{"message": {"content": "Run when a predator is close."}}]}
        text, ids = payload["prompt"], payload["allowed_token_ids"]
        z = [3.0 if f'- {a}: "Always' in text else 0.0 for a in options_of(text)]
        lse = math.log(sum(math.exp(x) for x in z))
        top = {f"token_id:{i}": x - lse for i, x in zip(ids, z)}
        return {"choices": [{"logprobs": {"top_logprobs": [top]}}], "usage": {"prompt_tokens": 2000}}
    return t


def jev_model(log, **kw):
    client = OpenAIClient("http://jev:8000", transport=fake_vllm(log), retries=1)
    return JevModel(client, head=HEAD, temps={"choice": 0.5}, **kw)


def backend(model, cache=None):
    return JevBackend(model, resolve("prompts/teacher_v5.md"), resolve("prompts/predator_v3.md"), cache=cache)


def test_text_matches_model_card_template():
    t = jev_text("choice", "S", "Q?", ["eat", "flee"])
    assert t == "[kind] choice\n[state] S\n[question] Q?\n[options]\nA) eat\nB) flee\n[decision]:"


def test_state_has_genes_and_situation_but_no_points_line():
    t = backend(jev_model([])).text(Query("g", GENES, OBS[3]))
    assert '- flee: "Always flee when you can."' in t
    assert "Distribute" not in t and "{" not in t
    assert t.endswith("A) eat\nB) flee\nC) follow\nD) rest\nE) mate\n[decision]:")


def test_request_shape_bias_and_temperature():
    log = []
    m = jev_model(log)
    z = m.choice_logits([backend(m).text(Query("g", GENES, OBS[0]))], 5)
    path, req = log[0]
    assert path == "/v1/completions" and req["model"] == "jev-decision" and req["max_tokens"] == 1
    assert req["allowed_token_ids"] == list(range(32, 37)) and req["add_special_tokens"] is False
    assert m.too_long == 1                                   # usage.prompt_tokens 2000 > 1024
    p = option_probs(z[0], 0.5)
    # A gets the head bias 0.5, B the gene; both / T 0.5: p ∝ exp(2 * [0.5, 3, 0, 0, 0])
    want = np.exp(2 * np.array([0.5, 3, 0, 0, 0]))
    assert np.allclose(p, want / want.sum())


def test_decide_reads_genes_dedups_and_caches():
    log, cache = [], KVCache()
    b = backend(jev_model(log), cache)
    qs = [Query("g", GENES, o) for o in OBS] + [Query("g", GENES, OBS[0])]
    p = b.decide(qs)
    assert p.shape == (5, len(ACTIONS)) and np.allclose(p.sum(1), 1)
    assert (p.argmax(1) == ACTIONS.index("flee")).all()
    assert b.calls == 4 and len(log) == 4                   # the repeated situation is asked once
    log2 = []
    b2 = backend(jev_model(log2), cache)
    assert np.allclose(b2.decide(qs), p) and log2 == []     # answers come from the cache


def test_predator_queries_use_predator_prompt_and_actions():
    genes = {a: "Always hunt." if a == "hunt" else "Rarely." for a in PREDATOR.actions}
    log = []
    p = backend(jev_model(log)).decide([Query("p", genes, OBS[0], "predator")])
    assert p.shape == (1, len(PREDATOR.actions)) and p[0].argmax() == 0
    prompt = log[0][1]["prompt"]
    assert "predator does next" in prompt and "D) mate\n[decision]:" in prompt
    assert log[0][1]["allowed_token_ids"] == list(range(32, 36))


def test_factory_knows_jev(cfg):
    b = make_backend("jev", cfg, model=jev_model([]))
    assert b.name == "jev" and b.info()["host"] == "http://jev:8000"
    assert b.info()["model"].endswith(":/models/JEV-9B/adapter_vllm")


def test_mutator_goes_through_openai_client_at_its_own_host():
    cfg = load_config("small", {"evolution": {"p_mut": 0.1},
                                "mutator": {"api": "openai", "host": "http://mut:9001", "model": "mut"}})
    c = mutator_client(cfg)
    assert isinstance(c, OpenAIClient) and c.host == "http://mut:9001"
    assert c.base_extra == {"chat_template_kwargs": {"enable_thinking": False}}
    log = []
    c._transport, c.cache = fake_vllm(log), KVCache()
    ask, model = make_rewriter(cfg, c, check=True)
    assert model == "mut" and ask("Rewrite: Flee.", 7) == "Run when a predator is close."
    path, req = log[0]
    assert path == "/v1/chat/completions" and req["seed"] == 7 and req["temperature"] == 1.2
    assert req["chat_template_kwargs"] == {"enable_thinking": False}
    ask("Rewrite: Flee.", 7)
    assert len(log) == 1                                     # the same (prompt, seed) is cached


def test_mutator_ollama_api_uses_mutator_host():
    cfg = load_config("small", {"mutator": {"api": "ollama", "host": "http://other:11434"}})
    assert mutator_client(cfg).host == "http://other:11434"


def test_gate_runs_on_the_jev_backend(cfg):
    from experiments.teacher_gate import run_gate
    log = []
    b = backend(jev_model(log))
    results, agreement = run_gate(cfg, None, ["jev"], 12, 1, None, b.model.id, logs=False, make=lambda m: b)
    r = results["jev"]
    assert r["calls"] > 0 and len(log) == b.calls and r["failures"] == r["calls"]   # fake prompts count as too long
    assert r["sign_acc"] > 0.9 and agreement is None
