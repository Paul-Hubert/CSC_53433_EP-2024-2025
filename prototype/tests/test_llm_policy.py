import json
import re

import numpy as np

from promptevo.backends.base import Query
from promptevo.backends.factory import make_backend, make_rewriter
from promptevo.backends.ollama_policy import LLMPolicyBackend
from promptevo.cache import KVCache
from promptevo.config import load_config, resolve
from promptevo.genome import ACTIONS
from promptevo.llm.ollama_client import OllamaClient, client_from_config
from promptevo.obs_text import render
from promptevo.perception import Observation
from promptevo.sim import Simulation

OBS = [Observation("low", f, p, "none") for f in ("none", "near") for p in ("none", "near")]


def points_for(genes: dict, situation: str) -> dict:
    pts = {a: 10 for a in ACTIONS}
    for a in ACTIONS:
        g = genes.get(a, "").lower()
        pts[a] = 80 if "always" in g else 1 if "never" in g else 10
    if "Predator: near" in situation:
        pts["flee"] += 40
    return pts


def fake(log, break_rows=()):
    def t(method, path, payload):
        if path == "/api/tags":
            return {"models": [{"name": "brain", "digest": "b" * 12}]}
        log.append(payload)
        prompt = payload["messages"][0]["content"]
        genes = dict(re.findall(r'- (\w+): "([^"]*)"', prompt))
        props = payload["format"]["properties"]
        if "s1" in props:                                   # table mode
            sits = dict(re.findall(r"^(s\d+): (.*)$", prompt, flags=re.M))
            out = {k: ({} if k in break_rows else points_for(genes, sits[k])) for k in props}
        elif "action" in props:
            p = points_for(genes, prompt)
            out = {"action": max(p, key=p.get)}
        else:
            out = points_for(genes, prompt)
        return {"message": {"content": json.dumps(out)}}
    return t


def _queries(reg, pools, genomes):
    return [Query(reg.genome_key(g), reg.genes(g), o) for g in genomes for o in OBS]


def test_table_mode_batches_and_matches_points(reg_pools):
    reg, pools = reg_pools
    pro, anti = pools.contrast_pair("flee")
    qs = _queries(reg, pools, [pro, anti])
    log_t, log_p = [], []
    table = LLMPolicyBackend(OllamaClient(transport=fake(log_t)), "brain", resolve("prompts/teacher_v1.md"),
                             mode="table", table_k=8, prefetch=False)
    points = LLMPolicyBackend(OllamaClient(transport=fake(log_p)), "brain", resolve("prompts/teacher_v1.md"),
                              mode="points")
    Pt, Pp = table.decide(qs), points.decide(qs)
    assert len(log_t) == 2 and len(log_p) == len(qs)          # one call per genome vs per query
    assert np.allclose(Pt, Pp)
    f = ACTIONS.index("flee")
    assert Pt[0, f] > Pt[len(OBS), f]                          # "always flee" > "never flee"
    near = [i for i, o in enumerate(OBS) if o.predator == "near"]
    far = [i for i, o in enumerate(OBS) if o.predator == "none"]
    assert Pt[len(OBS) + near[0], f] > Pt[len(OBS) + far[0], f]   # the situation matters too


def test_table_malformed_row_falls_back_to_single_call(reg_pools):
    reg, pools = reg_pools
    log = []
    b = LLMPolicyBackend(OllamaClient(transport=fake(log, break_rows=("s2",))), "brain",
                         resolve("prompts/teacher_v1.md"), mode="table", prefetch=False)
    P = b.decide(_queries(reg, pools, [pools.neutral_genome()]))
    assert len(log) == 2 and np.allclose(P.sum(1), 1)          # 1 table call + 1 repair call


def test_per_query_cache_survives_new_backend(reg_pools, tmp_path):
    reg, pools = reg_pools
    qs = _queries(reg, pools, [pools.neutral_genome()])
    log = []
    mk = lambda: LLMPolicyBackend(OllamaClient(transport=fake(log)), "brain",
                                  resolve("prompts/teacher_v1.md"), mode="table",
                                  cache=KVCache(tmp_path / "p.sqlite"))
    P1 = mk().decide(qs)
    n = len(log)
    P2 = mk().decide(qs[::-1])                                  # other order, new run
    assert len(log) == n and np.allclose(P1, P2[::-1])


def test_api_key_header_and_client_from_config(monkeypatch):
    seen = {}

    class Resp:
        def __enter__(self): return self
        def __exit__(self, *a): return False
        def read(self): return b'{"version": "x"}'

    def fake_urlopen(req, timeout):
        seen.update(dict(req.header_items()))
        return Resp()
    monkeypatch.setattr("urllib.request.urlopen", fake_urlopen)
    monkeypatch.setenv("OLLAMA_API_KEY", "secret-test")
    cfg = load_config("small", {"ollama": {"host": "https://ollama.com"}})
    c = client_from_config(cfg)
    c.cache = KVCache()
    assert c.version() == "x" and seen.get("Authorization") == "Bearer secret-test"
    monkeypatch.delenv("OLLAMA_API_KEY")
    assert client_from_config(cfg).api_key is None


def test_factory_and_llm_simulation(reg_pools):
    cfg = load_config("small", {"policy": {"model": "brain", "mode": "table"}, "ollama": {"mutator_model": "brain"},
                                "evolution": {"p_mut": 1.0, "operators": {
                                    "intensity": 0, "negate": 0, "condition_swap": 0, "synonym": 0,
                                    "founder_reintroduce": 0, "llm_rewrite": 1.0}}})
    assert make_backend("rule_based", cfg).name == "rule_based"
    log = []
    client = OllamaClient(transport=fake(log))
    backend = LLMPolicyBackend.from_config(cfg, client=client)
    backend.cache = None
    calls = []

    def rewriter(text, style, seed):
        calls.append(style)
        return "Run from every shadow."
    rw, model = make_rewriter(cfg, client)
    assert rw is not None and model == "brain"
    sim = Simulation(cfg, backend, seed=3, rewriter=rewriter, rewriter_model="fake")
    s = sim.run(300)
    assert s["decisions"] > 0 and backend.calls > 0
    # prefetch: most later decisions of a genome are answered by its earlier table call
    assert backend.calls < 0.7 * s["backend_queries"], (backend.calls, s["backend_queries"])
    assert backend.prefetched > 0
    sim._birth(sim.agents[:2])                                  # force one birth: genes via LLM
    child = sim.agents[-1]
    assert calls and "Run from every shadow." in sim.registry.genes(child.genome).values()


def lp_transport(log, tokens):
    def t(method, path, payload):
        if path == "/api/tags":
            return {"models": [{"name": "brain", "digest": "b" * 12}]}
        log.append(payload)
        if payload.get("logprobs"):
            return {"message": {"content": tokens[0][0]},
                    "logprobs": [{"token": tokens[0][0], "logprob": tokens[0][1],
                                  "top_logprobs": [{"token": tk, "logprob": lp} for tk, lp in tokens]}]}
        return {"message": {"content": json.dumps({a: 10 for a in ACTIONS})}}
    return t


def test_logprobs_mode_and_fallback(reg_pools):
    from promptevo.backends.ollama_policy import action_for_token
    assert action_for_token(" Flee") == "flee" and action_for_token("f") is None
    reg, pools = reg_pools
    q = _queries(reg, pools, [pools.neutral_genome()])[:1]
    log = []
    toks = [(" flee", np.log(0.6)), ("eat", np.log(0.3)), ("The", np.log(0.05))]
    b = LLMPolicyBackend(OllamaClient(transport=lp_transport(log, toks)), "brain",
                         resolve("prompts/teacher_v1.md"), mode="logprobs")
    p = b.decide(q)[0]
    assert p.argmax() == ACTIONS.index("flee") and abs(p[ACTIONS.index("eat")] - 1 / 3) < 0.01
    assert log[0]["options"]["num_predict"] == 1 and b.logprob_fallbacks == 0
    log2 = []                                         # server without logprobs → points fallback
    b2 = LLMPolicyBackend(OllamaClient(transport=fake(log2)), "brain",
                          resolve("prompts/teacher_v1.md"), mode="logprobs")
    p2 = b2.decide(q)[0]
    assert b2.logprob_fallbacks == 1 and abs(p2.sum() - 1) < 1e-9


def _flaky(log, fail_first: int):
    """Fake Ollama whose first `fail_first` chat calls raise (network error)."""
    inner = fake(log)

    def t(method, path, payload):
        if path == "/api/chat" and fail_first > len(log):
            log.append("fail")
            raise ConnectionError("server down")
        return inner(method, path, payload)
    return t


def test_failed_call_is_not_cached(reg_pools, tmp_path, monkeypatch):
    monkeypatch.setattr("promptevo.llm.ollama_client.time.sleep", lambda s: None)
    reg, pools = reg_pools
    q = _queries(reg, pools, [pools.contrast_pair("flee")[0]])[:1]   # "always flee": a skewed answer
    log, cache = [], KVCache(tmp_path / "p.sqlite")
    b = LLMPolicyBackend(OllamaClient(transport=_flaky(log, 1), retries=1), "brain",
                         resolve("prompts/teacher_v1.md"), mode="points", cache=cache)
    p1 = b.decide(q)[0]
    assert np.allclose(p1, 1 / len(ACTIONS)) and b.last_fallback == {0} and b.failures == 1
    assert len(cache) == 0                                     # the stand-in answer was not stored
    p2 = b.decide(q)[0]                                        # asked again, now answered for real
    assert not np.allclose(p2, 1 / len(ACTIONS)) and b.last_fallback == set() and len(cache) == 1


def test_malformed_reply_is_not_pinned_in_client_cache():
    replies = iter(["not json at all", json.dumps({"eat": 1})])
    sent = []

    def t(method, path, payload):
        if path == "/api/tags":
            return {"models": []}
        sent.append(payload)
        return {"message": {"content": next(replies)}}
    c = OllamaClient(transport=t)
    msgs = [{"role": "user", "content": "x"}]
    try:
        c.chat_json("m", msgs, POINTS_SCHEMA_FOR_TEST)
        raise AssertionError("malformed JSON should raise")
    except ValueError:
        pass
    assert c.chat_json("m", msgs, POINTS_SCHEMA_FOR_TEST) == {"eat": 1} and len(sent) == 2
    assert c.chat_json("m", msgs, POINTS_SCHEMA_FOR_TEST) == {"eat": 1} and len(sent) == 2  # cached now


POINTS_SCHEMA_FOR_TEST = {"type": "object"}


def test_simulation_does_not_memoise_fallback_rows(cfg):
    from promptevo.backends.rule_based import RuleBasedBackend

    class AlwaysFailing(RuleBasedBackend):
        def decide(self, queries):
            self.last_fallback = set(range(len(queries)))
            self.asked = getattr(self, "asked", 0) + len(queries)
            return np.full((len(queries), len(ACTIONS)), 1 / len(ACTIONS))
    b = AlwaysFailing()
    sim = Simulation(cfg, b, seed=1)
    sim.decide()
    first = b.asked
    assert sim.memo == {} and sim.c.fallbacks == first > 0
    assert all(a.action in ACTIONS for a in sim.agents)       # agents still act this tick
    sim.decide()
    assert b.asked == 2 * first                                 # nothing was served from the memo
