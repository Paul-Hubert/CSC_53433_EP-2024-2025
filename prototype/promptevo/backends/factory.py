"""One place that turns a backend name into a decision backend (and the LLM rewriter)."""
from __future__ import annotations


def make_backend(name: str, cfg, **over):
    if name == "rule_based":
        from .rule_based import RuleBasedBackend
        return RuleBasedBackend()
    if name == "random":
        from .random_policy import RandomBackend
        return RandomBackend()
    if name in ("llm", "teacher", "ollama_policy"):
        from .ollama_policy import LLMPolicyBackend
        return LLMPolicyBackend.from_config(cfg, **over)
    if name == "laya":                                   # parked (doc 05, rev. 2026-09-30)
        from .laya_backend import LayaBackend
        return LayaBackend.from_config(cfg, **{k: v for k, v in over.items() if k in ("placement", "style") and v})
    raise SystemExit(f"unknown backend {name!r} (random | rule_based | llm | laya)")


def make_rewriter(cfg, client=None):
    """(rewriter, model) for LLM gene mutation, or (None, None) if disabled/unconfigured."""
    model = cfg.ollama.mutator_model
    if not model or float(cfg.evolution.operators.get("llm_rewrite", 0)) <= 0:
        return None, None
    from ..config import resolve
    from ..llm.ollama_client import client_from_config, make_rewriter as _mk
    client = client or client_from_config(cfg)
    return _mk(client, model, resolve("prompts/mutate_v1.md"),
               max_words=int(cfg.evolution.max_action_words)), model
