"""Minimal local Ollama client (stdlib only) with caching and retries.

API: POST /api/chat, POST /api/embed, GET /api/tags, GET /api/version.
`transport(method, path, payload) -> dict` can be injected for tests.
"""
from __future__ import annotations

import json
import time
import urllib.request
from pathlib import Path
from typing import Callable

from ..cache import KVCache, make_key

Transport = Callable[[str, str, dict | None], dict]


class OllamaClient:
    def __init__(self, host: str = "http://localhost:11434", cache: KVCache | None = None,
                 timeout: float = 120, transport: Transport | None = None, retries: int = 3):
        self.host = host.rstrip("/")
        self.cache = cache or KVCache()
        self.timeout, self.retries = timeout, retries
        self._transport = transport or self._http
        self._digests: dict[str, str] = {}
        self.last_meta: dict = {}

    def _http(self, method: str, path: str, payload: dict | None) -> dict:
        data = json.dumps(payload).encode() if payload is not None else None
        req = urllib.request.Request(self.host + path, data=data, method=method,
                                     headers={"Content-Type": "application/json"})
        with urllib.request.urlopen(req, timeout=self.timeout) as r:
            return json.loads(r.read().decode())

    def _call(self, method: str, path: str, payload: dict | None = None) -> dict:
        err = None
        for i in range(self.retries):
            try:
                return self._transport(method, path, payload)
            except Exception as e:           # network hiccup / model loading
                err = e
                time.sleep(2 ** i)
        raise RuntimeError(f"Ollama {path} failed after {self.retries} tries: {err}")

    # --- info ------------------------------------------------------------------
    def version(self) -> str:
        return self._call("GET", "/api/version").get("version", "?")

    def models(self) -> list[dict]:
        return [{"name": m["name"], "digest": m.get("digest", "")[:12]}
                for m in self._call("GET", "/api/tags").get("models", [])]

    def digest(self, model: str) -> str:
        if model not in self._digests:
            for m in self.models():
                self._digests[m["name"]] = m["digest"]
                if m["name"].endswith(":latest"):
                    self._digests[m["name"].removesuffix(":latest")] = m["digest"]
            self._digests.setdefault(model, "unknown")
        return self._digests[model]

    # --- generation --------------------------------------------------------------
    def chat(self, model: str, messages: list[dict], schema: dict | None = None,
             options: dict | None = None, keep_alive: str | int | None = None,
             use_cache: bool = True, extra: dict | None = None) -> str:
        options = options or {}
        key = make_key("chat", model, self.digest(model), messages, schema, options, extra)
        if use_cache:
            hit = self.cache.get(key)
            if hit is not None:
                return hit
        payload = {"model": model, "messages": messages, "stream": False, "options": options}
        if schema is not None:
            payload["format"] = schema
        if keep_alive is not None:
            payload["keep_alive"] = keep_alive
        if extra:
            payload.update(extra)
        res = self._call("POST", "/api/chat", payload)
        self.last_meta = {k: res.get(k) for k in ("eval_count", "eval_duration", "prompt_eval_count",
                                                  "prompt_eval_duration", "total_duration")}
        self.last_meta["raw_keys"] = sorted(res)
        self.last_response = res
        content = res["message"]["content"]
        if use_cache:
            self.cache.put(key, content)
        return content

    def chat_json(self, model: str, messages: list[dict], schema: dict, **kw) -> dict:
        return json.loads(self.chat(model, messages, schema=schema, **kw))

    def embed(self, model: str, texts: list[str]) -> list[list[float]]:
        key = make_key("embed", model, self.digest(model), texts)
        hit = self.cache.get(key)
        if hit is not None:
            return hit
        emb = self._call("POST", "/api/embed", {"model": model, "input": texts})["embeddings"]
        self.cache.put(key, emb)
        return emb


def make_rewriter(client: OllamaClient, model: str, prompt_path: str | Path, max_words: int = 12,
                  temperature: float = 0.9):
    """Return rewriter(text, style, seed) -> str for evolution.mutation.Mutator."""
    template = Path(prompt_path).read_text()

    def rewrite(text: str, style: str, seed: int) -> str:
        prompt = template.format(style=style, text=text, max_words=max_words)
        return client.chat(model, [{"role": "user", "content": prompt}],
                           options={"seed": int(seed), "temperature": temperature})
    return rewrite
