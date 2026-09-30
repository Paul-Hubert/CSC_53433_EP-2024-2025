"""Minimal Ollama client (stdlib only) with caching and retries — local or cloud.

API: POST /api/chat, POST /api/embed, GET /api/tags, GET /api/version.
Two ways to use Ollama Cloud models (verify on your install, S1.3):
  * through the local server after `ollama signin`, with a cloud model tag
    (host stays http://localhost:11434), or
  * directly: host https://ollama.com + an API key in the env var named by
    cfg.ollama.api_key_env (sent as "Authorization: Bearer ..."). Never commit keys.
`transport(method, path, payload) -> dict` can be injected for tests.
"""
from __future__ import annotations

import json
import os
import time
import urllib.request
from pathlib import Path
from typing import Callable

from ..cache import KVCache, make_key

Transport = Callable[[str, str, dict | None], dict]


class OllamaClient:
    def __init__(self, host: str = "http://localhost:11434", cache: KVCache | None = None,
                 timeout: float = 120, transport: Transport | None = None, retries: int = 3,
                 api_key: str | None = None):
        self.host = host.rstrip("/")
        self.api_key = api_key
        self.cache = cache or KVCache()
        self.timeout, self.retries = timeout, retries
        self._transport = transport or self._http
        self._digests: dict[str, str] = {}
        self.last_meta: dict = {}

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
            try:
                listed = self.models()
            except RuntimeError:            # e.g. cloud endpoint without /api/tags access
                listed = []
            for m in listed:
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

    def chat_raw(self, model: str, messages: list[dict], options: dict | None = None,
                 extra: dict | None = None, keep_alive: str | int | None = None) -> dict:
        """Uncached call returning the whole response (e.g. to read logprobs)."""
        payload = {"model": model, "messages": messages, "stream": False, "options": options or {}}
        if keep_alive is not None:
            payload["keep_alive"] = keep_alive
        payload.update(extra or {})
        return self._call("POST", "/api/chat", payload)

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


def client_from_config(cfg, transport: Transport | None = None) -> OllamaClient:
    """The one place scripts build a client: host, API key (env), sqlite cache, timeout."""
    from ..cache import KVCache
    from ..config import resolve
    oc = cfg.ollama
    env = oc.get("api_key_env")
    key = os.environ.get(env) if env else None
    return OllamaClient(oc.host, KVCache(resolve(cfg.paths.cache_dir) / "ollama.sqlite"),
                        oc.timeout_s, transport=transport, api_key=key or None)


def make_rewriter(client: OllamaClient, model: str, prompt_path: str | Path, max_words: int = 12,
                  temperature: float = 0.9):
    """Return rewriter(text, style, seed) -> str for evolution.mutation.Mutator."""
    template = Path(prompt_path).read_text()

    def rewrite(text: str, style: str, seed: int) -> str:
        prompt = template.format(style=style, text=text, max_words=max_words)
        return client.chat(model, [{"role": "user", "content": prompt}],
                           options={"seed": int(seed), "temperature": temperature})
    return rewrite
