import json
import re

import numpy as np
import pytest

from promptevo.backends.base import Query
from promptevo.backends.factory import make_backend, make_rewriter
from promptevo.backends.ollama_policy import LLMPolicyBackend
from promptevo.cache import KVCache
from promptevo.config import load_config, resolve
from promptevo.genome import ACTIONS
from promptevo.llm.ollama_client import OllamaClient, client_from_config
from promptevo.founder import AllelePools
from promptevo.obs_text import render
from promptevo.perception import Observation, PredatorObservation
from promptevo.sim import Simulation
from promptevo.species import PREDATOR

OBS = [Observation("low", f, p, "none") for f in ("none", "close") for p in ("none", "close")]


def points_for(genes: dict, situation: str, actions=ACTIONS) -> dict:
    pts = {}
    for a in actions:
        g = genes.get(a, "").lower()
        pts[a] = 80 if "always" in g else 1 if "never" in g else 10
    if "flee" in pts and "Predator: 2-4 cells away" in situation:
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
            acts = list(props["s1"]["properties"])
            out = {k: ({} if k in break_rows else points_for(genes, sits[k], acts)) for k in props}
        elif "action" in props:
            p = points_for(genes, prompt, props["action"]["enum"])
            out = {"action": max(p, key=p.get)}
        else:
            out = points_for(genes, prompt, list(props))
        return {"message": {"content": json.dumps(out)}}
    return t


def _queries(reg, pools, genomes):
    return [Query(reg.genome_key(g), reg.genes(g), o) for g in genomes for o in OBS]


def test_table_mode_batches_and_matches_points(reg_pools):
    reg, pools = reg_pools
    pro, anti = pools.contrast_pair("flee")
    qs = _queries(reg, pools, [pro, anti])
    log_t, log_p = [], []
    table = LLMPolicyBackend(OllamaClient(transport=fake(log_t)), "brain", resolve("prompts/teacher_v5.md"),
                             mode="table", table_k=8, prefetch=False)
    points = LLMPolicyBackend(OllamaClient(transport=fake(log_p)), "brain", resolve("prompts/teacher_v5.md"),
                              mode="points")
    Pt, Pp = table.decide(qs), points.decide(qs)
    assert len(log_t) == 2 and len(log_p) == len(qs)          # one call per genome vs per query
    assert np.allclose(Pt, Pp)
    f = ACTIONS.index("flee")
    assert Pt[0, f] > Pt[len(OBS), f]                          # "always flee" > "never flee"
    near = [i for i, o in enumerate(OBS) if o.predator == "close"]
    far = [i for i, o in enumerate(OBS) if o.predator == "none"]
    assert Pt[len(OBS) + near[0], f] > Pt[len(OBS) + far[0], f]   # the situation matters too


def test_table_malformed_row_falls_back_to_single_call(reg_pools):
    reg, pools = reg_pools
    log = []
    b = LLMPolicyBackend(OllamaClient(transport=fake(log, break_rows=("s2",))), "brain",
                         resolve("prompts/teacher_v5.md"), mode="table", prefetch=False)
    P = b.decide(_queries(reg, pools, [pools.neutral_genome()]))
    assert len(log) == 2 and np.allclose(P.sum(1), 1)          # 1 table call + 1 repair call


def test_per_query_cache_survives_new_backend(reg_pools, tmp_path):
    reg, pools = reg_pools
    qs = _queries(reg, pools, [pools.neutral_genome()])
    log = []
    mk = lambda: LLMPolicyBackend(OllamaClient(transport=fake(log)), "brain",
                                  resolve("prompts/teacher_v5.md"), mode="table",
                                  cache=KVCache(tmp_path / "p.sqlite"))
    P1 = mk().decide(qs)
    n = len(log)
    P2 = mk().decide(qs[::-1])                                  # other order, new run
    assert len(log) == n and np.allclose(P1, P2[::-1])


def test_failed_call_is_never_cached_and_strict_raises(reg_pools, tmp_path, monkeypatch):
    monkeypatch.setattr("promptevo.llm.ollama_client.time.sleep", lambda s: None)
    reg, pools = reg_pools
    qs = _queries(reg, pools, [pools.neutral_genome()])

    def down(method, path, payload):
        if path == "/api/tags":
            return {"models": [{"name": "brain", "digest": "b" * 12}]}
        raise ConnectionRefusedError("no server")
    cache = KVCache(tmp_path / "p.sqlite")
    mk = lambda **kw: LLMPolicyBackend(OllamaClient(transport=down, retries=1), "brain",
                                       resolve("prompts/teacher_v5.md"), cache=cache, **kw)
    b = mk()
    P = b.decide(qs)                                            # not strict: uniform for this decision only
    assert np.allclose(P, 1 / len(ACTIONS)) and b.failures == len(qs)
    assert len(cache) == 0 and not b._mem                       # ... and never remembered
    with pytest.raises(RuntimeError):
        mk(strict=True).decide(qs)
    cfg = load_config("small", {"policy": {"model": "brain"}})
    assert make_backend("llm", cfg, strict=True).strict and not make_backend("llm", cfg).strict


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
    cfg = load_config("small", {"policy": {"model": "brain", "mode": "table"}, "mutator": {"model": "brain"},
                                "evolution": {"p_mut": 1.0}})
    assert make_backend("rule_based", cfg).name == "rule_based"
    log = []
    client = OllamaClient(transport=fake(log))
    backend = LLMPolicyBackend.from_config(cfg, client=client)
    backend.cache = None
    calls = []

    def rewriter(prompt, seed):
        calls.append(prompt)
        return re.search(r'"(.*)"', prompt).group(1).rstrip(".") + " quickly."   # a small edit
    rw, model = make_rewriter(cfg, client)
    assert rw is not None and model == "brain"
    sim = Simulation(cfg, backend, seed=3, rewriter=rewriter, rewriter_model="fake")
    s = sim.run(300)
    assert s["decisions"] > 0 and s["predators"]["decisions"] > 0 and backend.calls > 0
    # prefetch: many later decisions of a genome are answered by its earlier table call
    # (0.73 of the queries at 300 ticks since stamina made situations more varied)
    queries = s["backend_queries"] + s["predators"]["backend_queries"]
    assert backend.calls < 0.8 * queries, (backend.calls, queries)
    assert backend.prefetched > 0
    sim._birth(sim.agents[:2])                                  # force one birth: genes via LLM
    child = sim.agents[-1]
    assert calls and any(t.endswith(" quickly.") for t in sim.registry.genes(child.genome).values())


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
                         resolve("prompts/teacher_v5.md"), mode="logprobs")
    p = b.decide(q)[0]
    assert p.argmax() == ACTIONS.index("flee") and abs(p[ACTIONS.index("eat")] - 1 / 3) < 0.01
    assert log[0]["options"]["num_predict"] == 1 and b.logprob_fallbacks == 0
    log2 = []                                         # server without logprobs → points fallback
    b2 = LLMPolicyBackend(OllamaClient(transport=fake(log2)), "brain",
                          resolve("prompts/teacher_v5.md"), mode="logprobs")
    p2 = b2.decide(q)[0]
    assert b2.logprob_fallbacks == 1 and abs(p2.sum() - 1) < 1e-9


def test_empty_persistent_cache_is_used(tmp_path):
    """An empty KVCache has len 0: the client must still write to it, not to memory."""
    cache = KVCache(tmp_path / "o.sqlite")
    client = OllamaClient(cache=cache, transport=lambda method, path, payload: (
        {"models": []} if path == "/api/tags" else {"message": {"content": "hi"}}))
    assert client.cache is cache
    client.chat("brain", [{"role": "user", "content": "x"}])
    assert len(KVCache(tmp_path / "o.sqlite")) == 1


def test_make_rewriter_off_and_unreachable(monkeypatch):
    from promptevo.backends.factory import MutatorUnavailable
    assert make_rewriter(load_config("small", {"mutator": {"model": None}})) == (None, None)
    assert make_rewriter(load_config("small", {"evolution": {"p_mut": 0.0}})) == (None, None)
    monkeypatch.setattr("promptevo.llm.ollama_client.time.sleep", lambda s: None)

    def down(method, path, payload):
        raise ConnectionRefusedError("no server")
    with pytest.raises(MutatorUnavailable):
        make_rewriter(load_config("small"), OllamaClient(transport=down, retries=1), check=True)


def test_predator_queries_use_their_own_prompt_and_actions(reg_pools, cfg):
    reg, prey_pools = reg_pools
    pools = AllelePools(reg, cfg.paths.data_dir, species=PREDATOR)
    log = []
    mk = lambda **kw: LLMPolicyBackend(OllamaClient(transport=fake(log)), "brain",
                                       resolve("prompts/teacher_v5.md"), **kw)
    b = mk(predator_prompt_path=resolve("prompts/predator_v3.md"))
    pro, anti = pools.contrast_pair("hunt")
    o = PredatorObservation("low", "close", "none")
    qs = [Query(reg.genome_key(g), reg.genes(g), o, "predator") for g in (pro, anti)]
    P = b.decide(qs)
    assert P.shape == (2, len(PREDATOR.actions)) and P[0, 0] > P[1, 0]   # "always hunt" > "never hunt"
    prompt = log[0]["messages"][0]["content"]
    assert prompt.startswith("You decide what a predator does next") and '- hunt: "Always hunt' in prompt
    assert "Situation: Energy: low. Prey: 2-4 cells away. Other predator: none within 20 cells." in prompt
    assert list(log[0]["format"]["properties"]) == list(PREDATOR.actions)
    prey_q = _queries(reg, prey_pools, [prey_pools.neutral_genome()])[0]
    assert b._key(prey_q) != b._key(qs[0])
    with pytest.raises(ValueError):                                 # one species per batch
        b.decide([prey_q, qs[0]])
    with pytest.raises(ValueError):                                 # no predator prompt configured
        mk().decide(qs)
