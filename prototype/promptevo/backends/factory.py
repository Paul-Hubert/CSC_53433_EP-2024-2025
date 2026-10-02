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


class MutatorUnavailable(RuntimeError):
    """Gene mutation is configured but the Ollama server can't be reached."""


def make_rewriter(cfg, client=None, check: bool = False):
    """(llm, model) for gene mutation, or (None, None) when mutation is off
    (ollama.mutator_model is null or evolution.p_mut is 0). check=True asks the server
    once and raises MutatorUnavailable instead of failing at the first birth."""
    model = cfg.ollama.mutator_model
    if not model or float(cfg.evolution.p_mut) <= 0:
        return None, None
    from ..llm.ollama_client import client_from_config, make_rewriter as _mk
    client = client or client_from_config(cfg)
    if check:
        try:
            client.version()
        except RuntimeError as e:
            raise MutatorUnavailable(f"gene mutation uses the Ollama model {model} at {cfg.ollama.host}, "
                                     f"but the server didn't answer ({e})") from e
    return _mk(client, model, temperature=float(cfg.evolution.temperature)), model
