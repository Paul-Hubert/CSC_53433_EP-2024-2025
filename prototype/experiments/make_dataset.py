"""S4.5 — build the distillation dataset (keys only; labels come from label_teacher.py).

    python -m experiments.make_dataset --profile small      # 3 000 rows
    python -m experiments.make_dataset --profile full       # 10 000 rows

Allele split per locus (plan S4.5):
  train = founder alleles + neutral + mutants of the other founders
  val   = mutants of one founder allele        (text never seen in train)
  test  = mutants of another founder allele + OOD novel alleles
Contrast alleles (directed tests) are never used. Control (random-text) sentences
are split 50/25/25 so gibberish genomes in val/test are unseen too.

Row kinds:
  contrast    — base genome + 3 variants differing at ONE locus, same observations
                (forces the student to read that gene)
  regular     — one genome, a few observations
  random_text — every gene is random text (teaches "gibberish ≈ neutral")
Outputs data/dataset_v1_{train,val,test}.jsonl and data/dataset_v1_meta.json.
"""
from __future__ import annotations

import argparse
import json
from collections import Counter
from dataclasses import asdict

import numpy as np

from experiments.make_obs import load_obs, synthetic
from promptevo.cache import make_key
from promptevo.config import load_config, resolve
from promptevo.founder import AllelePools
from promptevo.genome import LOCI, AlleleRegistry
from promptevo.perception import RELEVANT_TAG, Observation

SIZES = {"small": 3000, "full": 10000}
SPLIT_FRAC = {"train": 0.8, "val": 0.1, "test": 0.1}
KIND_FRAC = {"contrast": 0.5, "regular": 0.4, "random_text": 0.1}
OBS_PER_GENOME = 3
VARIANTS = 3


def split_alleles(reg, pools, mutants: list[dict], rng) -> dict:
    """{split: {locus: [texts]}} plus which founder parents were held out."""
    out = {s: {l: [] for l in LOCI} for s in SPLIT_FRAC}
    held = {}
    for l in LOCI:
        founders = [reg.text(a) for a in pools.founders[l] if a != pools.neutral[l]]
        val_p, test_p = [founders[i] for i in rng.choice(len(founders), 2, replace=False)]
        held[l] = {"val_parent": val_p, "test_parent": test_p}
        out["train"][l] = founders + [reg.text(pools.neutral[l])]
        for m in mutants:
            if m["locus"] != l:
                continue
            if m["origin"] == "ood" or m["parent"] == test_p:
                out["test"][l].append(m["text"])
            elif m["parent"] == val_p:
                out["val"][l].append(m["text"])
            else:
                out["train"][l].append(m["text"])
        # a mutant can reproduce a training text exactly: held-out lists must stay unseen
        train = {t.lower() for t in out["train"][l]}
        out["val"][l] = [t for t in dict.fromkeys(out["val"][l]) if t.lower() not in train]
        val = {t.lower() for t in out["val"][l]}
        out["test"][l] = [t for t in dict.fromkeys(out["test"][l]) if t.lower() not in train | val]
        out["train"][l] = list(dict.fromkeys(out["train"][l]))
    return {"alleles": out, "held_parents": held}


def split_controls(texts: list[str], rng) -> dict:
    idx = rng.permutation(len(texts))
    n = len(texts)
    a, b = int(0.5 * n), int(0.75 * n)
    return {"train": [texts[i] for i in idx[:a]], "val": [texts[i] for i in idx[a:b]],
            "test": [texts[i] for i in idx[b:]]}


class Builder:
    def __init__(self, alleles: dict, controls: dict, founder_texts: dict, obs_pool: list, rng):
        self.alleles, self.controls, self.founders = alleles, controls, founder_texts
        self.obs, self.rng = obs_pool, rng

    def _pick(self, xs):
        return xs[int(self.rng.integers(len(xs)))]

    def base_genome(self, split: str) -> tuple[dict, list[str]]:
        """train: any train allele per locus. val/test: founder genome with 1–3 held-out loci."""
        if split == "train":
            return {l: self._pick(self.alleles["train"][l]) for l in LOCI}, []
        g = {l: self._pick(self.founders[l]) for l in LOCI}
        avail = [l for l in LOCI if self.alleles[split][l]]
        k = min(len(avail), int(self.rng.integers(1, 4)))
        loci = [avail[i] for i in self.rng.choice(len(avail), k, replace=False)] if k else []
        for l in loci:
            g[l] = self._pick(self.alleles[split][l])
        return g, loci

    def pick_obs(self, n: int, tag: str | None = None) -> list[Observation]:
        pool = [o for o in self.obs if tag is None or tag in o.tags()] or self.obs
        idx = self.rng.choice(len(pool), min(n, len(pool)), replace=False)
        return [pool[i] for i in idx]

    def rows_regular(self, split):
        g, held = self.base_genome(split)
        return [dict(kind="regular", genes=g, held_out_loci=held, obs=o) for o in self.pick_obs(OBS_PER_GENOME)]

    def rows_random_text(self, split):
        g = {l: self._pick(self.controls[split]) for l in LOCI}
        return [dict(kind="random_text", genes=g, held_out_loci=[], obs=o) for o in self.pick_obs(OBS_PER_GENOME)]

    def rows_contrast(self, split, group: int):
        g, held = self.base_genome(split)
        cands = [l for l in LOCI if len(set(self.alleles[split][l]) - {g[l]}) >= 1]
        locus = self._pick(cands)
        options = sorted(set(self.alleles[split][locus]) - {g[locus]})
        variants = [options[i] for i in self.rng.choice(len(options), min(VARIANTS, len(options)), replace=False)]
        obs = self.pick_obs(OBS_PER_GENOME, RELEVANT_TAG.get(locus))
        rows = []
        for text in [g[locus]] + variants:
            gv = dict(g, **{locus: text})
            is_held = split != "train" and text in self.alleles[split][locus]
            hv = sorted((set(held) - {locus}) | ({locus} if is_held else set()))
            rows += [dict(kind="contrast", group=group, varied_locus=locus, genes=gv, held_out_loci=hv, obs=o)
                     for o in obs]
        return rows


def build(cfg, reg, pools, mutants, obs_pool, n_total: int, seed: int):
    rng = np.random.default_rng(seed)
    sa = split_alleles(reg, pools, mutants, rng)
    controls = split_controls(pools.control_texts, rng)
    founder_texts = {l: [reg.text(a) for a in pools.founders[l]] for l in LOCI}
    b = Builder(sa["alleles"], controls, founder_texts, obs_pool, rng)
    rows, seen, group = [], set(), 0
    for split, frac in SPLIT_FRAC.items():
        target = int(n_total * frac)
        counts = Counter()
        stall = 0
        while sum(counts.values()) < target and stall < 200:
            made = sum(counts.values())
            kind = max(KIND_FRAC, key=lambda k: KIND_FRAC[k] * target - counts[k])   # most behind
            if kind == "contrast":
                group += 1
                new = b.rows_contrast(split, group)
            elif kind == "regular":
                new = b.rows_regular(split)
            else:
                new = b.rows_random_text(split)
            for r in new:
                genes = [r["genes"][l] for l in LOCI]
                obs = asdict(r["obs"])
                key = make_key("row", genes, obs)
                if key in seen:
                    continue
                seen.add(key)
                r.update(key=key, split=split, obs=obs)
                r.setdefault("group", None)
                r.setdefault("varied_locus", None)
                rows.append(r)
                counts[kind] += 1
            stall = stall + 1 if sum(counts.values()) == made else 0
    meta = {"seed": seed, "n_total": len(rows), "held_parents": sa["held_parents"],
            "alleles": sa["alleles"], "controls": controls,
            "counts": {s: dict(Counter(r["kind"] for r in rows if r["split"] == s)) for s in SPLIT_FRAC}}
    return rows, meta


def load_rows(split: str, data_dir="data", version="v1") -> list[dict]:
    p = resolve(data_dir) / f"dataset_{version}_{split}.jsonl"
    return [json.loads(l) for l in p.read_text().splitlines()]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--profile", default="small")
    ap.add_argument("--n", type=int, default=None)
    ap.add_argument("--mutants", default="data/mutants_v1.jsonl")
    ap.add_argument("--version", default="v1")
    a = ap.parse_args()
    cfg = load_config(a.profile)
    reg = AlleleRegistry()
    pools = AllelePools(reg, cfg.paths.data_dir)
    mutants = [json.loads(l) for l in resolve(a.mutants).read_text().splitlines()]
    obs_pool = list(dict.fromkeys(load_obs() + synthetic()))
    rows, meta = build(cfg, reg, pools, mutants, obs_pool, a.n or SIZES[a.profile], cfg.seed + 202)
    d = resolve(cfg.paths.data_dir)
    for split in SPLIT_FRAC:
        with (d / f"dataset_{a.version}_{split}.jsonl").open("w") as f:
            for r in rows:
                if r["split"] == split:
                    f.write(json.dumps(r, ensure_ascii=False) + "\n")
    (d / f"dataset_{a.version}_meta.json").write_text(json.dumps(meta, indent=1, ensure_ascii=False))
    held = {s: sum(len(v) for v in meta["alleles"][s].values()) for s in SPLIT_FRAC}
    print(f"wrote {len(rows)} rows → {d}/dataset_{a.version}_*.jsonl (obs pool {len(obs_pool)})")
    print("rows by split/kind:", meta["counts"])
    print("distinct alleles by split:", held)


if __name__ == "__main__":
    main()
