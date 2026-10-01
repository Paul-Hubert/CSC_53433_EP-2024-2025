"""S1.3 — probe Ollama (local server, or cloud). Run on your machine:
    python -m experiments.e0_probe_ollama --teacher <model> [--mutator <model>] [--embed <model>]
    OLLAMA_API_KEY=... python -m experiments.e0_probe_ollama --host https://ollama.com --teacher <cloud-model>
"--teacher" is the decision model (policy.model).
Writes results/e0_ollama.md. Copy facts into STATUS.md › API facts — Ollama.
"""
from __future__ import annotations

import argparse
import json
import os
import time
import traceback

from promptevo.backends.ollama_policy import POINTS_SCHEMA, logprobs_to_probs, teacher_prompt
from promptevo.config import resolve
from promptevo.llm.ollama_client import OllamaClient

GENES = {"eat": "Eat whenever food is close.", "flee": "Always run away, whatever happens.",
         "follow": "Stay close to other animals.", "wander": "Keep moving to new places.",
         "rest": "Rest when you are tired.", "mate": "Look for a partner when energy is high.",
         "attack": "Never fight.", "risk": "Cautious: safety comes before food.",
         "social": "Social: feels safer in a group.", "place": "Prefers staying near water."}
SITUATION = "Energy: low. Food: near. Predator: near. Animal: none. Age: adult."


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="http://localhost:11434")
    ap.add_argument("--teacher", required=True)
    ap.add_argument("--mutator", default=None)
    ap.add_argument("--embed", default=None)
    ap.add_argument("--num-ctx", type=int, default=4096, help="0 = server default")
    ap.add_argument("--think", choices=["off", "on", "default"], default="off")
    a = ap.parse_args()
    c = OllamaClient(a.host, api_key=os.environ.get("OLLAMA_API_KEY"), timeout=600,
                     options={"num_ctx": a.num_ctx} if a.num_ctx else None,
                     think={"off": False, "on": True, "default": None}[a.think])
    out, facts = [], {}

    def section(title, fn):
        out.append(f"\n## {title}\n")
        try:
            r = fn()
            out.append("```\n" + json.dumps(r, indent=1, default=str) + "\n```")
            facts[title] = r
        except Exception as e:
            out.append(f"**FAILED** `{e!r}`\n```\n{traceback.format_exc()[-800:]}\n```")
            facts[title] = f"FAILED {e!r}"

    section("1. Version & models", lambda: {"version": c.version(), "models": c.models()})
    prompt = teacher_prompt(open(resolve("prompts/teacher_v1.md")).read(), GENES, SITUATION, "points")
    msgs = [{"role": "user", "content": prompt}]

    def structured():
        r = c.chat_json(a.teacher, msgs, POINTS_SCHEMA, options={"seed": 1, "temperature": 0}, use_cache=False)
        m = c.last_meta
        tps = m["eval_count"] / (m["eval_duration"] / 1e9) if m.get("eval_duration") else None
        pps = m["prompt_eval_count"] / (m["prompt_eval_duration"] / 1e9) if m.get("prompt_eval_duration") else None
        return {"points": r, "gen_tok_s": tps and round(tps, 1), "prompt_tok_s": pps and round(pps, 1),
                "prompt_tokens": m.get("prompt_eval_count"), "total_s": m.get("total_duration", 0) / 1e9}
    section("2. Teacher structured output (points) + speed", structured)

    def determinism():
        rs = [c.chat(a.teacher, msgs, schema=POINTS_SCHEMA, options={"seed": 7, "temperature": 0}, use_cache=False)
              for _ in range(3)]
        return {"identical_3x": len(set(rs)) == 1}
    section("3. seed+temperature 0 determinism", determinism)

    def logprobs():
        p = teacher_prompt(open(resolve("prompts/teacher_v1.md")).read(), GENES, SITUATION, "logprobs")
        res = c.chat_raw(a.teacher, [{"role": "user", "content": p}],
                         options={"seed": 1, "temperature": 0, "num_predict": 1},
                         extra={"logprobs": True, "top_logprobs": 10})
        probs = logprobs_to_probs(res)
        raw = res.get("logprobs") or (res.get("message") or {}).get("logprobs")
        return {"response_keys": sorted(res), "answer": res.get("message", {}).get("content"),
                "raw_first": json.dumps(raw[0])[:400] if isinstance(raw, list) and raw else None,
                "parsed_distribution": None if probs is None else [round(float(x), 3) for x in probs],
                "logprobs_mode_usable": probs is not None}
    section("4. Logprobs (policy.mode=logprobs usable?)", logprobs)

    def timing():
        out = {}
        for mode in ("points", "logprobs"):
            p = teacher_prompt(open(resolve("prompts/teacher_v1.md")).read(), GENES, SITUATION, mode)
            m = [{"role": "user", "content": p}]
            ts = []
            for i in range(3):
                t = time.perf_counter()
                if mode == "points":
                    c.chat(a.teacher, m, schema=POINTS_SCHEMA, options={"seed": i, "temperature": 0}, use_cache=False)
                else:
                    c.chat_raw(a.teacher, m, options={"seed": i, "temperature": 0, "num_predict": 1},
                               extra={"logprobs": True, "top_logprobs": 10})
                ts.append(time.perf_counter() - t)
            out[mode] = f"median {sorted(ts)[1]:.2f} s/decision"
        return out
    section("4b. Seconds per decision by mode (warm)", timing)

    if a.mutator:
        from promptevo.llm.ollama_client import make_rewriter
        rw = make_rewriter(c, a.mutator, resolve("prompts/mutate_v1.md"))
        section("5. Mutator samples", lambda: {s: rw("Eat whenever food is close.", s, 3)
                                               for s in ("random change", "invert", "add a condition")})
    if a.embed:
        section("6. Embeddings", lambda: {"dim": len(c.embed(a.embed, ["run from predators"])[0])})

    path = resolve("results/e0_ollama.md")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("# E0 — Ollama probe\n" + "\n".join(out) + "\n")
    print(f"wrote {path}")
    print(json.dumps(facts, default=str)[:1500])


if __name__ == "__main__":
    main()
