"""Smoke tests against the REAL local models. Skipped by default; run locally with
    pytest -m laya      (needs `pip install laya` + weights)
    pytest -m ollama    (needs Ollama — local or cloud — and policy.model set)
"""
import pytest

from promptevo.backends.base import Query
from promptevo.genome import ACTIONS
from promptevo.perception import Observation

O = Observation("low", "close", "close", "none")


@pytest.mark.laya
def test_real_laya_contrast(cfg, reg_pools):
    from promptevo.backends.laya_backend import LayaBackend
    reg, pools = reg_pools
    b = LayaBackend.from_config(cfg)
    pro, anti = pools.contrast_pair("flee")
    p = b.decide([Query(reg.genome_key(g), reg.genes(g), O) for g in (pro, anti)])
    assert p.shape == (2, len(ACTIONS)) and abs(p.sum(1) - 1).max() < 1e-6
    print("P(flee) pro/anti:", p[:, ACTIONS.index("flee")], "approx:", b.approx)


@pytest.mark.ollama
def test_real_llm_brain(cfg, reg_pools):
    """Needs policy.model set (local tag, or cloud via OLLAMA_API_KEY + ollama.host)."""
    from promptevo.backends.ollama_policy import LLMPolicyBackend
    if not (cfg.policy.model or cfg.ollama.teacher_model):
        pytest.skip("set policy.model in configs/base.yaml (S1.3)")
    reg, pools = reg_pools
    b = LLMPolicyBackend.from_config(cfg)
    b.cache = None
    pro, anti = pools.contrast_pair("flee")
    p = b.decide([Query(reg.genome_key(g), reg.genes(g), O) for g in (pro, anti)])
    print("P(flee) always/never:", p[:, ACTIONS.index("flee")], "calls", b.calls,
          "logprob_fallbacks", b.logprob_fallbacks)
    assert p[0, ACTIONS.index("flee")] > p[1, ACTIONS.index("flee")]
