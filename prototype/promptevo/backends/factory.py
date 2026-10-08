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
    if name == "jev":                                    # JEV-9B System 1 on a vLLM server (rev. 2026-10-08)
        from .jev_backend import JevBackend
        return JevBackend.from_config(cfg, **{k: v for k, v in over.items() if k in ("model", "style") and v})
    if name == "laya":                                   # parked (doc 05, rev. 2026-09-30)
        from .laya_backend import LayaBackend
        return LayaBackend.from_config(cfg, **{k: v for k, v in over.items() if k in ("placement", "style") and v})
    raise SystemExit(f"unknown backend {name!r} (random | rule_based | llm | jev | laya)")


class MutatorUnavailable(RuntimeError):
    """Gene mutation is configured but the mutator server can't be reached."""


def mutator_client(cfg):
    """HTTP client for gene mutation at mutator.host: an OpenAI-compatible server (vLLM,
    mutator.api openai, rev. 2026-10-08) or Ollama (mutator.api ollama)."""
    from ..config import resolve
    mc = cfg.mutator
    if mc.api == "openai":
        from ..llm.openai_client import openai_client
        return openai_client(mc, resolve(cfg.paths.cache_dir) / "mutator.sqlite")
    if mc.api == "ollama":
        from ..cache import KVCache
        from ..llm.ollama_client import OllamaClient
        return OllamaClient(mc.host, KVCache(resolve(cfg.paths.cache_dir) / "ollama.sqlite"),
                            float(mc.get("timeout_s") or 120), options=dict(mc.get("options") or {}),
                            think=mc.get("think"))
    raise SystemExit(f"unknown mutator.api {mc.api!r} (openai | ollama)")


def make_rewriter(cfg, client=None, check: bool = False):
    """(llm, model) for gene mutation, or (None, None) when mutation is off
    (mutator.model is null or evolution.p_mut is 0). check=True asks the server
    once and raises MutatorUnavailable instead of failing at the first birth."""
    model = cfg.mutator.model
    if not model or float(cfg.evolution.p_mut) <= 0:
        return None, None
    from ..llm.ollama_client import make_rewriter as _mk
    client = client or mutator_client(cfg)
    if check:
        try:
            client.version()
        except RuntimeError as e:
            raise MutatorUnavailable(f"gene mutation uses the model {model} at {cfg.mutator.host} "
                                     f"({cfg.mutator.api} API), but the server didn't answer ({e})") from e
    return _mk(client, model, temperature=float(cfg.evolution.temperature)), model
