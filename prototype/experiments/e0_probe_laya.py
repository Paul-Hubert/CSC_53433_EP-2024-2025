"""S1.2 — probe the installed Laya package. Run locally:
    python -m experiments.e0_probe_laya [--checkpoint convaiinnovations/laya] [--subfolder multilingual]
Writes results/e0_laya_api.md; prints a short summary. Every probe is isolated
(a failure is recorded, not fatal). Copy the facts into STATUS.md › API facts — Laya.
"""
from __future__ import annotations

import argparse
import json
import platform
import statistics
import time
import traceback

from promptevo.backends.laya_backend import build_request, extract_probs
from promptevo.config import resolve

SITUATION = "Energy: low. Food: near. Predator: far. Animal: none. Age: adult."
GENES = {"eat": "Eat whenever food is close.", "flee": "Run from any predator you see.",
         "follow": "Stay close to other animals.", "rest": "Rest when you are tired.",
         "mate": "Look for a partner when energy is high."}


def shorten(obj, depth=0):
    if isinstance(obj, dict):
        return {k: shorten(v, depth + 1) for k, v in list(obj.items())[:20]}
    if isinstance(obj, list):
        return [shorten(v, depth + 1) for v in obj[:10]]
    if isinstance(obj, float):
        return round(obj, 4)
    if isinstance(obj, str) and len(obj) > 80:
        return obj[:80] + "…"
    return obj


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--checkpoint", default="convaiinnovations/laya")
    ap.add_argument("--subfolder", default=None)
    ap.add_argument("--repeats", type=int, default=5)
    a = ap.parse_args()
    out, facts = [], {}

    def section(title, fn):
        out.append(f"\n## {title}\n")
        try:
            res = fn()
            out.append("```\n" + (res if isinstance(res, str) else json.dumps(res, indent=1, default=str)) + "\n```")
            return res
        except Exception as e:
            out.append(f"**FAILED**: `{e!r}`\n```\n{traceback.format_exc()[-1200:]}\n```")
            facts[title] = f"FAILED {e!r}"
            return None

    import laya
    kw = {"subfolder": a.subfolder} if a.subfolder else {}
    t0 = time.perf_counter()
    agent = laya.load(a.checkpoint, **kw)
    load_s = time.perf_counter() - t0

    def versions():
        import torch
        return {"python": platform.python_version(), "laya": getattr(laya, "__version__", "?"),
                "torch": torch.__version__, "cuda": torch.cuda.is_available(),
                "device": str(getattr(agent, "device", "?")), "load_s": round(load_s, 1),
                "agent_type": type(agent).__name__,
                "public_attrs": [x for x in dir(agent) if not x.startswith("_")][:60]}
    section("1. Versions & agent", versions)

    state, questions = build_request(GENES, SITUATION, "P4")
    raw = section("2. predict() raw result (P4)", lambda: shorten(agent.predict(state, questions)))

    def probs():
        res = agent.predict(state, questions)
        p, exact = extract_probs(res["answers"]["action"])
        facts["per_option_probs"] = exact
        return {"probs": [round(x, 4) for x in p], "exact_per_option": exact}
    section("2b. extract_probs on it", probs)

    def criteria_effect():
        base = extract_probs(agent.predict(state, questions)["answers"]["action"])[0]
        g2 = dict(GENES, flee="Always run away, whatever happens.")
        s2, q2 = build_request(g2, SITUATION.replace("Predator: far", "Predator: near"), "P4")
        s1, q1 = build_request(GENES, SITUATION.replace("Predator: far", "Predator: near"), "P4")
        p1 = extract_probs(agent.predict(s1, q1)["answers"]["action"])[0]
        p2 = extract_probs(agent.predict(s2, q2)["answers"]["action"])[0]
        return {"P(flee) founder gene": round(float(p1[1]), 4), "P(flee) 'always run' gene": round(float(p2[1]), 4),
                "max |diff| across options": round(float(abs(p1 - p2).max()), 4), "base": [round(x, 3) for x in base]}
    section("3. Does criteria text change scores?", criteria_effect)

    def tokenizer():
        for path in ("tokenizer", "model.tokenizer", "encoder.tokenizer", "_tokenizer"):
            obj = agent
            try:
                for part in path.split("."):
                    obj = getattr(obj, part)
                n = len(obj(json.dumps(questions))["input_ids"])
                return {"found_at": f"agent.{path}", "tokens(question json)": n,
                        "head_max_len": getattr(agent, "head_max_len", getattr(getattr(agent, "config", None), "head_max_len", "?")),
                        "max_len": getattr(agent, "max_len", "?")}
            except Exception:
                continue
        return "no tokenizer attribute found — use transformers AutoTokenizer (see e0_budget.py)"
    section("4. Tokenizer & budgets", tokenizer)

    def overflow():
        long_genes = {k: (v + " ") * 12 for k, v in GENES.items()}
        s, q = build_request(long_genes, SITUATION, "P4")
        res = agent.predict(s, q)
        return {"no error on 12x genes": True, "result_keys": sorted(res)}
    section("4b. Overflow behaviour (12x long genes)", overflow)

    def batch_api():
        cands = [x for x in dir(agent) if "batch" in x.lower() or x in ("predict_many", "__call__")]
        tried = {}
        try:
            r = agent.predict([state, state], questions)
            tried["predict(list_of_states)"] = type(r).__name__
        except Exception as e:
            tried["predict(list_of_states)"] = f"error {type(e).__name__}"
        return {"batch-like attrs": cands, "tried": tried}
    section("5. Batch API", batch_api)

    def determinism():
        ps = [extract_probs(agent.predict(state, questions)["answers"]["action"])[0] for _ in range(3)]
        return {"identical": all(abs(ps[0] - p).max() < 1e-7 for p in ps)}
    section("6. Determinism", determinism)

    def latency():
        rows = {}
        for label, genes_ in [("7 options, short state", GENES)]:
            s, q = build_request(genes_, SITUATION, "P4")
            agent.predict(s, q)
            ts = []
            for _ in range(a.repeats):
                t = time.perf_counter(); agent.predict(s, q); ts.append(time.perf_counter() - t)
            rows[label] = f"median {statistics.median(ts)*1000:.1f} ms"
        q2 = {"yn": {"type": "noul", "instructions": "Is food near?"}}
        ts = []
        for _ in range(a.repeats):
            t = time.perf_counter(); agent.predict({"situation": SITUATION}, q2); ts.append(time.perf_counter() - t)
        rows["1 noul question"] = f"median {statistics.median(ts)*1000:.1f} ms"
        s, q = build_request(GENES, SITUATION + " " + "Nothing else happens here. " * 30, "P4")
        ts = []
        for _ in range(a.repeats):
            t = time.perf_counter(); agent.predict(s, q); ts.append(time.perf_counter() - t)
        rows["7 options, long state (~200 tok)"] = f"median {statistics.median(ts)*1000:.1f} ms"
        facts["latency"] = rows
        return rows
    section("7. Latency", latency)

    section("8. ONNX / temperature", lambda: {
        "ONNXAgent": hasattr(laya, "ONNXAgent"),
        "temperature-like attrs": [x for x in dir(agent) if "temp" in x.lower() or "calib" in x.lower()]})

    path = resolve("results/e0_laya_api.md")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(f"# E0 — Laya probe ({a.checkpoint} {a.subfolder or ''})\n" + "\n".join(out) + "\n")
    print(f"wrote {path}")
    print(json.dumps(facts, default=str)[:1500])


if __name__ == "__main__":
    main()
