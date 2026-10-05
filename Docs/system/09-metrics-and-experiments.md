# 09 — Metrics & experiments

Every metric in `metrics.py`, the E1 suite and the decision-model gate, the planned experiments E0–E5, conditions C1–C7, gates G1–G5, and the current key numbers.

## Notation

`p(a | o, g)` is the backend's action distribution for observation `o` and genome
`g`. Arrays are `P[g, o, a]`. `H` is entropy in **bits**. All metrics are in
`prototype/promptevo/metrics.py` (✅, unit-tested on synthetic distributions).

## Sensitivity metrics

| Function | Formula | Meaning |
|---|---|---|
| `entropy(p)` | `−Σ_a p log₂ p` (p clipped at 1e-12) | uncertainty of one distribution; max log₂7 ≈ 2.81 |
| `jsd(p, q)` | `H(m) − ½(H(p) + H(q))`, `m = ½(p + q)` | Jensen–Shannon divergence, 0…1 bits |
| `mi_genome(P)` → **MI_G** | `mean_o [ H(mean_g p(·\|o,g)) − mean_g H(p(·\|o,g)) ]` | how much the genome determines the action, given the situation; 0 if all genomes behave alike |
| `mi_obs(P)` → **MI_O** | `mean_g [ H(mean_o p(·\|o,g)) − mean_o H(p(·\|o,g)) ]` | how much the situation determines the action; must stay high or agents ignore their world |
| `behaviour_distance(Pa, Pb)` | `mean_o JSD(p(·\|o,g), p(·\|o,g′))`, inputs `[O, A]` | behavioural difference between two genomes |
| `directed(P_pro, P_anti, a, relevant)` | `d = P_pro[o, a] − P_anti[o, a]` over relevant `o`; returns `mean_dp`, `sign_acc = mean(d > 0)`, `n` | **directed test / ΔP**: does the "pro" gene raise its own action vs the "anti" gene? |
| `locality(edit_d, unrelated_d)` | medians and `ratio = median(edit_d) / median(unrelated_d)` | small edits should give small behaviour changes (ratio ≪ 1) |
| `spearman(x, y)` | Pearson correlation of average-tie ranks; `nan` if a side is constant | does text distance predict behaviour distance? |
| `shannon_diversity(counts)` | `H(counts / Σ counts)` over non-zero counts | allele diversity at a locus (bits) |
| `bootstrap_ci(x, n=2000, alpha=0.05)` | `(mean, lower, upper)` from resampled means | confidence intervals (common garden, G4) |

MI_G and MI_O are mutual informations between genome (resp. observation) and
action, estimated with uniform weights over the sets used.

## The E1 suite (`experiments/e1_sensitivity.py`)

Works with any backend (`--backend rule_based|llm|laya|random`). Status ✅ for
`rule_based`, 🧪 for `llm`.

**Genome sets** (`build_sets`, rng `default_rng(cfg.seed)`):

| Set | small | full |
|---|---|---|
| founders (sampled, deduplicated) | 30 | 60 |
| random-text controls (`sample_control`) | 30 | 60 |
| neutral genome | 1 | 1 |
| contrast pairs | 7 × 2 | 7 × 2 |
| single-edit children: parents × edits per parent (word operators only) | 10 × 4 | 20 × 6 |

Observations: `data/observations_v1.jsonl` (48 in small; plan 08 says 150 for
full, not generated yet). Every genome is evaluated on every observation.

**How each result is computed** (`evaluate`):

- **Directed:** per action locus, `directed(pro, anti, locus, mask)` with
  `mask = RELEVANT_TAG[locus] ∈ obs.tags()`; overall `sign_acc` is weighted by
  `n`, `mean_dp` is the unweighted mean over loci.
- **MI_G founders**, **MI_G random**, **MI_O founders**.
- **Locality:** `edit_d` = behaviour distance parent → single-edit child;
  `unrelated_d` = all founder pairs. **Text distance** =
  `1 − difflib.SequenceMatcher(" | ".join(texts)).ratio()` (normalised edit
  similarity; plan 08 prefers embedding cosine, not implemented).
  `rho_text_behaviour` = Spearman over edits + founder pairs together.
- **Gibberish:** `gibberish_to_neutral` = mean distance random-text → neutral;
  compared with `founder_to_neutral`.

Outputs `results/e1_<tag>.json` and `.md`; progress in `logs/e1_<tag>.progress.json`.

**Gate check in the script** (`GATES`):

| Gate | Condition in code |
|---|---|
| G1 | `sign_acc ≥ 0.85` and `mean_dp ≥ 0.25` |
| G2 | `MI_G founders ≥ 0.25` and `MI_O founders ≥ 0.25` and `gibberish_to_neutral ≤ 0.5 × founder_to_neutral` |
| G3 | `locality ratio ≤ 0.5` and `ρ ≥ 0.3` |

## Decision-model gate (`experiments/teacher_gate.py`) 🧪

Since rev. 2026-09-30 the main G1/G2 check for the LLM brain. Per mode
(`--modes`, default `points,table`): 10 founders, 10 random-text genomes,
neutral, contrast pairs, × `--n-obs` (24) observations picked so every tag is
represented (up to 3 per tag), no edits. Pass = `sign_acc ≥ 0.85` **and**
`MI_G founders ≥ 2 × max(MI_G random, 0.001)`. With two modes it also reports
mode agreement (mean JSD). Writes `results/teacher_gate.md` + `.json`. Plan:
iterate on the prompt or model at most 3 times, logging each in STATUS (H3).

## Evolution metrics (plan 08 §A9)

| Metric | Available now |
|---|---|
| population, births, deaths by cause, lifespan, generations | ✅ `stats.csv`, `summary.json`, `death` events |
| allele frequencies per locus over time | derivable from `events.jsonl` + `alleles.jsonl`; no analysis code (☐ `analyze.py`) |
| per-locus Shannon diversity and allele count | `shannon_diversity()` exists; not wired to runs (☐) |
| mean gene length in words (bloat) | ☐ |
| mutant establishment rate per operator (fraction of mutant alleles reaching ≥ 5 % frequency) | ☐ |
| crossover blending, teacher agreement (KL) | ☐ (teacher agreement belongs to the parked distillation) |

### Common garden (E5) ☐

Each genome group (founders sampled at t = 0 vs living genomes from the last
20 % of a run) runs alone in K = 10 fixed arenas, with no reproduction,
T = 1 500 ticks and N = 20 agents. Scores: mean survival ticks and food eaten,
compared with bootstrap CIs. Not built (`experiments/common_garden.py`).

## Experiments (plan 08 §A10, revised 2026-09-30)

| ID | What | Status |
|---|---|---|
| E0 | Probes: Ollama API facts (models, structured output, seed determinism, logprobs, s/decision by mode, mutator samples); Laya probe and token budget parked | 🧪 `e0_probe_ollama.py` written, not run; 💤 Laya parts |
| E1 | Gene-sensitivity suite (E1a input formats was Laya-specific; now `--backend llm`) | ✅ rule-based reference · ☐ on the LLM |
| E2 | Throughput: seconds per decision, memo hit rate, calls per 1 000 ticks | ☐ (`e2_bench.py` not written; smoke run gives partial numbers) |
| E3 | Teacher gate → dataset → distillation → held-out eval | 💤 parked except the gate, which became the decision-model gate |
| E4 | Evolution matrix: conditions below, 20 k ticks per run, 3 / 5 seeds (revision: default 10 k ticks, D = 8, 3 seeds) | ☐ (`run_matrix.py`) |
| E5 | Common garden (+ optional competition assay) | ☐ |

### E4 conditions

All share the world seed; simulation seeds vary. Switches: [06](06-evolution.md#experimental-controls).

| Condition | Definition |
|---|---|
| C1 FULL | LLM brain (rev. 2026-09-30; originally fine-tuned Laya), sexual, mixed mutation, selection |
| C2 NO-MUT | `p_mut = 0`: selection on founder variation only |
| C3 SHUFFLED | decisions use a random other living agent's genome: drift only |
| C4 RANDOM-FOUNDERS | founders from the control (random-text) alleles |
| C5 RULE-BASED | C1 with the `rule_based` backend |
| C6 ZERO-SHOT | zero-shot Laya — dropped in the revision |
| C7 ASEXUAL | one parent, copy + mutation (the old lab's regime), optional |

## Gates (plan 08 §A2, preregistered)

The owner may edit thresholds only **before** S6 launches the evolution matrix.

| Gate | Pass if | Implemented |
|---|---|---|
| **G1 Semantics** | directed sign accuracy ≥ 85 % and mean ΔP ≥ 0.25 | E1, gate script |
| **G2 Information** | MI_G ≥ 0.25 bits and MI_O ≥ 0.25 bits; gibberish distance to neutral ≤ ½ founder-to-neutral | E1 (the gate script uses the 2× margin instead) |
| **G3 Locality** | median single-edit distance ≤ 0.5 × median unrelated-founder distance, and Spearman ρ(text, behaviour) ≥ 0.3 | E1 |
| **G4 Evolution** | common-garden survival evolved > founders (bootstrap 95 % CI of the difference > 0) in ≥ 4/5 seeds, and a larger gain than the shuffled control | ☐ |
| **G5 Throughput** | effective decisions/s (with cache) ≥ 50 on GPU and ≥ 10 on CPU at population 40 | ☐ (now the main risk) |

Plan text says G1 is measured on held-out alleles and G2 on a test split; both
referred to the parked distillation. With the LLM brain they are measured on the
E1 sets directly.

| Outcome | Next step (plan 08 §A2, Laya wording kept) |
|---|---|
| All pass | Unity with per-decision brain (option A) as the default |
| G1–G4 pass, G5 fails on CPU | Option B (model at birth) for students; option A on GPU or a lab server |
| G1 or G2 fails | Fallback ladder (§A12: stronger prompt, bigger model, other mode); if still failing, option B with a generative LLM at birth |
| G3 fails (chaotic map) | Restrict mutation to word operators, shorten genes, reduce loci, re-test |
| G4 fails, G1–G3 pass | The world is the problem: raise selection pressure, lengthen runs, check immigration is not swamping selection |

**Open question — G2 threshold.** Even the "ideal" keyword interpreter scores
MI_G = 0.23 < 0.25 (STATUS, 2026-09-28). The owner must decide before S6 whether
the threshold is too strict (progress log 09, "Decisions waiting on the owner"
item 3).

## Current key numbers

All offline, small profile, from `prototype/STATUS.md` and `results/`.

**Rule-based E1 reference** (2026-09-28, `results/e1_rule_based.md`; 114
genomes × 48 observations = 5 472 evaluations):

| Metric | Value |
|---|---|
| MI_G founders | 0.229 bits |
| MI_G random text | 0.000 |
| MI_O founders | 0.829 |
| Directed sign accuracy | 1.00 |
| Directed mean ΔP | 0.484 (eat 0.641, flee 0.811, follow 0.389, wander 0.616, rest 0.383, mate 0.281, attack 0.270) |
| Locality ratio | 0.00 (median edit distance ≈ 6e-6) |
| Spearman ρ | 0.45 |
| Gibberish → neutral / founder → neutral | 0.000 / 0.068 |
| Gates in script | G1 pass, G2 fail (MI_G), G3 pass |

The G3 pass is not informative for `rule_based`: most word edits do not touch
its keywords, so edits change nothing (STATUS).

**Simulation** (2026-09-28): 5 000 ticks ≈ 12 s CPU with `rule_based`; memo hit
rate ≈ 0.75; population ≈ 28, 24 generations per 5 000 ticks, lifespan ≈ 300.

**LLM load** (2026-09-30, fake near-random LLM): 2 000 ticks → 5 349 decisions →
2 211 LLM answers in points mode (0.41 per decision); table + prefetch K = 8:
1 185 requests, 9 342 answers. Estimate: 13–22 k LLM answers per 20 k-tick
small run, i.e. 4–6 h at ~1 s/answer — the main cost risk. Real seconds per
decision: unknown until the S1.3 probe.
