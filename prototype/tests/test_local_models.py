"""Smoke tests against the REAL local models. Skipped by default; run locally with
    pytest -m laya      (needs `pip install laya` + weights)
    pytest -m ollama    (needs `ollama serve` and cfg.ollama.teacher_model set)
"""
import pytest

from promptevo.backends.base import Query
from promptevo.genome import ACTIONS
from promptevo.perception import Observation

O = Observation("low", "near", "near", "none")


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
def test_real_ollama_teacher(cfg, reg_pools):
    from promptevo.backends.ollama_policy import TeacherBackend
    from promptevo.config import resolve
    from promptevo.llm.ollama_client import OllamaClient
    if not cfg.ollama.teacher_model:
        pytest.skip("set ollama.teacher_model in configs/base.yaml (S1.3)")
    reg, pools = reg_pools
    t = TeacherBackend(OllamaClient(cfg.ollama.host), cfg.ollama.teacher_model,
                       resolve("prompts/teacher_v1.md"))
    pro, anti = pools.contrast_pair("flee")
    p = t.decide([Query(reg.genome_key(g), reg.genes(g), O) for g in (pro, anti)])
    assert p[0, ACTIONS.index("flee")] > p[1, ACTIONS.index("flee")]
