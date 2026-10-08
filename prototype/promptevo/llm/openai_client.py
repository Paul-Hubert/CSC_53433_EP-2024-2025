"""Minimal OpenAI-compatible client (stdlib only) for the vLLM servers (rev. 2026-10-08).

Two servers, each with its own address (configs/base.yaml): `jev.host` (decisions: the JEV
System 1 LoRA, /v1/completions with allowed_token_ids + logprobs) and `mutator.host` (gene
mutation: a small chat model, /v1/chat/completions). Same surface as OllamaClient where the
rest of the code needs it: chat(model, messages, options={seed, temperature}) -> str (cached),
version(), digest(model). `transport(method, path, payload) -> dict` can be injected for tests.
"""
from __future__ import annotations

import json
import os
import time
import urllib.request
from typing import Callable

from ..cache import KVCache, make_key

Transport = Callable[[str, str, dict | None], dict]


class OpenAIClient:
    def __init__(self, host: str = "http://localhost:8000", cache: KVCache | None = None,
                 timeout: float = 120, transport: Transport | None = None, retries: int = 3,
                 api_key: str | None = None, extra: dict | None = None):
        self.host = host.rstrip("/")
        self.cache = cache if cache is not None else KVCache()   # an empty KVCache is falsy (__len__)
        self.timeout, self.retries, self.api_key = timeout, retries, api_key
        self.base_extra = dict(extra or {})     # sent with every chat call, e.g. chat_template_kwargs
        self._transport = transport or self._http
        self._models: dict[str, str] | None = None

    def _http(self, method: str, path: str, payload: dict | None) -> dict:
        data = json.dumps(payload).encode() if payload is not None else None
        headers = {"Content-Type": "application/json"}
        if self.api_key:
            headers["Authorization"] = f"Bearer {self.api_key}"
        req = urllib.request.Request(self.host + path, data=data, method=method, headers=headers)
        with urllib.request.urlopen(req, timeout=self.timeout) as r:
            return json.loads(r.read().decode())

    def _call(self, method: str, path: str, payload: dict | None = None) -> dict:
        err = None
        for i in range(self.retries):
            try:
                return self._transport(method, path, payload)
            except Exception as e:           # network hiccup / server starting
                err = e
                time.sleep(2 ** i)
        raise RuntimeError(f"{self.host}{path} failed after {self.retries} tries: {err}")

    # --- info ------------------------------------------------------------------
    def version(self) -> str:
        return self._call("GET", "/version").get("version", "?")

    def models(self) -> dict[str, str]:
        """Served model names -> what they load (vLLM: `root`, the base path or the LoRA path)."""
        if self._models is None:
            self._models = {m["id"]: str(m.get("root") or m["id"]) for m in self._call("GET", "/v1/models")["data"]}
        return self._models

    def digest(self, model: str) -> str:
        """Cache-key part for a model: what the server says it loads (pin revisions in the config)."""
        try:
            return self.models().get(model, "unknown")
        except RuntimeError:
            return "unknown"

    # --- generation --------------------------------------------------------------
    def chat(self, model: str, messages: list[dict], options: dict | None = None,
             use_cache: bool = True, max_tokens: int = 64, **_ignored) -> str:
        options = dict(options or {})           # seed, temperature (Ollama-style names)
        key = make_key("oai-chat", model, self.digest(model), messages, options, self.base_extra, max_tokens)
        if use_cache:
            hit = self.cache.get(key)
            if hit is not None:
                return hit
        payload = {"model": model, "messages": messages, "max_tokens": max_tokens, **self.base_extra}
        for k in ("seed", "temperature", "top_p"):
            if k in options:
                payload[k] = options[k]
        content = self._call("POST", "/v1/chat/completions", payload)["choices"][0]["message"]["content"] or ""
        if use_cache:
            self.cache.put(key, content)
        return content

    def token_logprobs(self, model: str, prompt: str, token_ids: list[int]) -> tuple[dict[int, float], int | None]:
        """(log-probabilities of the given tokens as the next token after `prompt`, prompt tokens).
        One prefill pass, no text kept. Uncached: the decision backend caches whole answers."""
        r = self._call("POST", "/v1/completions", {
            "model": model, "prompt": prompt, "max_tokens": 1, "temperature": 1.0,
            "logprobs": len(token_ids), "allowed_token_ids": list(token_ids),
            "add_special_tokens": False, "return_tokens_as_token_ids": True})
        top = r["choices"][0]["logprobs"]["top_logprobs"][0]
        return ({int(k.split(":")[1]): float(v) for k, v in top.items()},
                (r.get("usage") or {}).get("prompt_tokens"))


def openai_client(section, cache_path=None, transport: Transport | None = None) -> OpenAIClient:
    """Client for one config section with host / timeout_s / api_key_env / extra (jev, mutator)."""
    env = section.get("api_key_env")
    key = os.environ.get(env) if env else None
    cache = KVCache(cache_path) if cache_path else None
    return OpenAIClient(section.host, cache, float(section.get("timeout_s") or 120), transport=transport,
                        api_key=key or None, extra=dict(section.get("extra") or {}))
