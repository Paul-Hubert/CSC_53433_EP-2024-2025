# 08 — Phase 0 spike: local Python test (full plan + session runbook)

> **Goal:** find out, with numbers, whether *"genes = text prompts, an LLM
> decides, an LLM mutates"* works **before** any Unity work.
>
> **Local only:** **Laya** runs in-process for decisions. **Local Ollama**
> (`localhost:11434`) is the teacher, the mutator and the embedder. No
> cloud calls.
>
> **Assumes:** `pip install laya` works and an Ollama server is running with
> at least one instruct model pulled.
>
> **Where to run:** Claude Code **on the machine that has Laya + Ollama**
> (terminal or desktop app), launched from `prototype/`. A cloud session
> can't reach your local Ollama.

> ### Revision 2026-09-30: LLM brain instead of Laya
> The decision backend is now an **Ollama LLM** (local or cloud), called
> `llm` in code (see [05 §0](05-decision-backend.md)). It also mutates the
> genes. What changes in this plan:
>
> | Section | Change |
> |---|---|
> | S1.2 Laya probe | optional (skip unless Laya comes back) |
> | S1.3 Ollama probe | **key step**: pick `policy.model` (the brain) and `ollama.mutator_model`; check whether logprobs are returned and the seconds per decision by mode; local vs cloud |
> | S3 | E1 zero-shot on Laya is replaced by `e1_sensitivity --backend llm`; E2 benchmark = seconds per decision, memo hit rate, calls per 1 000 ticks |
> | S4.3 | `teacher_gate` is now the **decision-model gate** (G1/G2 for the brain itself; modes `points,table` or `points,logprobs`) |
> | S4.5–S4.6, S5 | **parked** (dataset, labelling, distillation). Code kept for a later "distil the LLM into a fast model" project |
> | S6 | C1 FULL uses `--backend llm`; C6 ZERO-SHOT is dropped. Expect LLM cost to dominate: see [05 §0](05-decision-backend.md) budget; default to 10 k ticks, D = 8, 3 seeds |
> | A2 gates | unchanged. G1–G3 are measured on the LLM brain; G5 (throughput) is now the main risk |
>
> Where the text below says "Laya", read "the decision backend" unless the
> section is marked parked.

**How to use this document**

- **Part A** is the spec: what we build and what we measure.
- **Part B** is the runbook: session by session, with `/compact` points and
  copy-paste prompts.
- Claude reads **only** the Part B session it is executing, plus the Part A
  sections that session lists. Never load the whole file.
- Handoff state lives in [`prototype/STATUS.md`](../../prototype/STATUS.md).
- Standing rules for Claude live in [`prototype/CLAUDE.md`](../../prototype/CLAUDE.md),
  which is auto-loaded when Claude Code starts in `prototype/`.

---

# Part A — Spec

## A1. Questions the spike answers

| # | Question | Experiment |
|---|---|---|
| Q1 | Does gene text change Laya's decisions **in the intended direction**? First zero-shot, then after distillation. | E1, E3 |
| Q2 | Is the gene → behaviour map **graded**? (small text edit → small behaviour change; children resemble their parents) | E1b, E3 |
| Q3 | Does a local teacher LLM express the genes, and can Laya learn that from it? | E3 |
| Q4 | Does evolution actually improve genomes compared with controls? | E4, E5 |
| Q5 | Does LLM mutation keep diversity, or does it collapse or bloat? | E4 analysis |
| Q6 | How fast is it on CPU and GPU? Which Laya checkpoint and input layout should we use? | E0, E1a, E2 |

## A2. Go / no-go gates

These are preregistered. The owner may edit the thresholds, but only
**before** S6 launches the evolution matrix. Definitions are in A9.

| Gate | Pass if | Measured on |
|---|---|---|
| **G1 Semantics** | directed-test sign accuracy ≥ 85 % **and** mean ΔP ≥ 0.25 | held-out alleles (never seen in training) |
| **G2 Information** | MI_G ≥ 0.25 bits **and** MI_O ≥ 0.25 bits; gibberish genomes stay near neutral (distance to neutral ≤ ½ of founder-to-neutral) | test split |
| **G3 Locality** | median distance of single-gene edits ≤ 0.5 × median distance between unrelated founders, **and** Spearman ρ(text distance, behaviour distance) ≥ 0.3 | LLM + word mutants |
| **G4 Evolution** | common-garden survival: evolved > founders (bootstrap 95 % CI of the difference > 0) in ≥ 4/5 seeds, **and** a larger gain than the shuffled control | E4 + E5 |
| **G5 Throughput** | effective decisions/s (with cache) ≥ 50 on GPU and ≥ 10 on CPU, at population 40 | E2 + E4 pilots |

| Outcome | Next step |
|---|---|
| All pass | Unity with Laya-per-decision (option A) as the default |
| G1–G4 pass, G5 fails on CPU | Option B (Laya at birth) for students; option A on GPU or a lab server |
| G1 or G2 fails after fine-tuning | Go through the fallback ladder (A12). If still failing, switch to option B with a generative LLM at birth |
| G3 fails (chaotic map) | Restrict mutation to word-level operators, shorten genes, reduce loci, then re-test |
| G4 fails but G1–G3 pass | The world is the problem, not the model: raise selection pressure, lengthen runs, check that immigration isn't swamping selection |

## A3. Project layout

The `prototype/` folder sits at the repo root. Unity only imports `Assets/`,
so it is ignored by Unity.

```
prototype/
  CLAUDE.md  STATUS.md  .gitignore  pyproject.toml
  configs/        base.yaml, small.yaml, full.yaml, exp_*.yaml
  prompts/        teacher_v1.md, mutate_v1.md
  data/           founder_pool_v1.json, contrast_alleles_v1.json, control_alleles_v1.json,
                  observations_v1.jsonl, mutants_v1.jsonl, dataset_v1_*.jsonl (+ labels)
  promptevo/
    config.py rng.py cache.py progress.py
    genome.py founder.py world.py perception.py obs_text.py actions.py
    backends/  base.py random_policy.py rule_based.py laya_backend.py ollama_policy.py
    llm/       ollama_client.py
    evolution/ crossover.py mutation.py reproduction.py
    sim.py metrics.py eventlog.py render.py
  experiments/    status.py peek.py e0_*.py make_obs.py e1_sensitivity.py e2_bench.py
                  teacher_gate.py make_mutants.py make_dataset.py label_teacher.py
                  finetune_laya.py e3_eval.py run_matrix.py common_garden.py analyze.py report.py
  tests/
  cache/  logs/  models/  results/runs/     (git-ignored)
  results/        *.md, *.png, small *.csv  (committed)
```

Dependencies: Python ≥ 3.10, `laya`, `numpy`, `pandas`, `scipy`,
`matplotlib`, `pyyaml`, `pytest`, `requests` (or the `ollama` client).

Every experiment script takes `--profile small|full` and prints a summary
of ≤ 20 lines.

## A4. World (headless grid)

These are starting values. S2.6 tunes them.

- **Terrain:** 64 × 64 grid (small profile: 48 × 48), from seeded value
  noise.
  - Cells below sea level are water (~15 %); cells above peak level are
    mountains (~10 %). Both are impassable.
  - Regenerate if the walkable area is not one connected region covering
    ≥ 60 % of the grid.
- **Food:** grows on grass cells, regrowing with probability `p_grow` per
  tick. The rate is ×2 within 3 cells of water: rich zones, but exposed ones.
- **Predators:** 3–5, scripted, never evolved. They do a persistent random
  walk, chase agents within radius 6, and kill on contact with
  probability 0.5.
- **Agents:**
  - Energy: max 100, starts at 60.
  - Cost per tick: 0.5 base, +0.5 when moving, 0.2 when resting.
  - Eating gives +25.
  - Maturity at 150 ticks, maximum age 1 500.
- **Decisions:** synchronised every **D = 4 ticks**. The sim runs in
  **lockstep**: it waits for the backend, so a slow machine is slower but
  gets identical results. The chosen behaviour runs for D ticks.
- **Reproduction:**
  - Two mature agents within 1 cell of each other, both in `mate`, both
    with energy ≥ 50, produce a child. The child starts with 40 energy,
    paid by the parents.
  - Births are blocked at the population cap (40 small / 60 full).
  - **Floor:** if the population falls below 10, founder genomes are
    spawned. These are logged as *immigration*.
- **Randomness:** all random draws come from named, seeded substreams
  (`world`, `predators`, `agents`, `sampling`, `mutation`).
- **ASCII renderer** (downsampled to ≤ 48 × 24 characters), used for
  debugging:
  - `~` water, `^` mountain, `.` food, `W` predator.
  - Each agent is shown by the first letter of its current action.

## A5. Genome

Ten loci, in a fixed order (this matches [04](04-genome-and-evolution.md)
and [05 §4](05-decision-backend.md)).

| Loci | Kind | Where it goes in Laya's input (P4) |
|---|---|---|
| `eat` `flee` `follow` `wander` `rest` `mate` `attack` | action gene, ≤ 12 words | the criteria text of that option |
| `risk` `social` `place` | temperament gene, ≤ 15 words | a preamble at the start of the state |

- **Allele registry:** each allele has an `id`, `locus`, `text`, `origin`
  (founder / contrast / control / mutant), `parent_id`, `operator`,
  `model`, `seed`.
- **Genome:** a tuple of 10 allele ids. Its hash is the sha256 of the
  allele texts.
- **`founder_pool_v1.json`:** 4 instinct-style alleles plus 1 neutral
  allele per locus (5¹⁰ ≈ 10 M combinations).
  - Style: short imperative, plain words, no numbers, e.g. *"Eat whenever
    food is close."*
  - Frozen and versioned once the owner approves it (H1).
- **`contrast_alleles_v1.json`:** per action locus, one **pro** allele
  (*"Always eat, whatever happens."*) and one **anti** allele (*"Never eat
  unless starving."*). Used for directed tests only, never as founders.
- **`control_alleles_v1.json`:** length-matched random text, of two kinds:
  - shuffled words;
  - fluent but irrelevant sentences (*"The museum opens at nine on
    weekdays."*).
- **Crossover:** uniform per locus. Mutation is described in A8.

## A6. Observation & actions

**Observation fields** (discretised):

| Field | Values |
|---|---|
| energy | low / medium / high |
| food | none / far / near / here |
| predator | none / far / near |
| other animal | none / far / near; if near, also: ready to mate yes/no, weaker/stronger |
| age | young / adult |

"Near" means ≤ 3 cells; "far" means ≤ 12 cells, the vision radius. This
gives only a few hundred distinct observations. As a result, a decision
cache keyed by *(genome hash, observation text)* hits very often.

**Text styles:**

- **V1 terse:** `Energy: low. Food: near. Predator: none. Animal: near, ready to mate, weaker.`
- **V2 first-person:** `I am hungry. Food is close. No predator in sight. …`

**Actions.** Each executor runs for D ticks.

| Action | Behaviour |
|---|---|
| `eat` | Move to the nearest food; eat it on arrival. |
| `flee` | Step away from the nearest predator. |
| `follow` | Step toward the nearest animal. |
| `wander` | Persistent random walk. |
| `rest` | Stay put, at low energy cost. |
| `mate` | Approach the nearest ready partner; breed if both chose `mate`. |
| `attack` | Approach; if adjacent, steal up to 10 energy with probability `E_self / (E_self + E_target)`. Costs 3 energy. |

An invalid choice (e.g. `eat` with no food visible) becomes `wander` and is
logged.

## A7. Decision backends

All backends implement one protocol:
`decide(batch: list[(Genome, Observation)]) -> probs[B, 7]`.

| Backend | Role |
|---|---|
| `random` | Floor baseline. |
| `rule_based` | A transparent "ideal interpreter". Sensible default logits, plus keyword parsing of genes: always / often / sometimes / rarely / never, and conditions such as *when hungry* or *when threatened*. Gibberish has no effect. It is a fast reference for pipeline tests and a control condition. |
| `laya` | The system under test, zero-shot or fine-tuned. Placements P1–P4 × styles V1/V2. |
| `ollama_policy` | The teacher used as a policy. Slow, so small runs only; it serves as an upper reference. |

**Laya placements** (where the genes go in Laya's input):

| ID | Genes go… | Options carry… |
|---|---|---|
| P1 | action genes in the option criteria; state = situation only | the genes |
| P2 | all genes in the state (`Instincts: …`) | fixed neutral descriptions |
| P3 | genes in the question instructions | fixed neutral descriptions (probably over budget; test only if it fits) |
| **P4** | P1, plus temperament genes as a state preamble (**default candidate**) | the genes |

Example P4 request:

```python
state = {"animal": "Temperament: <risk>. <social>. <place>.",
         "situation": "Energy: low. Food: near. Predator: far. Animal: none."}
questions = {"action": {"type": "choice",
    "instructions": "Which action does this animal take now?",
    "criteria": {"eat": "<eat gene>", "flee": "<flee gene>", "follow": "<follow gene>",
                 "wander": "<wander gene>", "rest": "<rest gene>",
                 "mate": "<mate gene>", "attack": "<attack gene>"}}}
```

- **Sampling:** the action is drawn from the returned probabilities using
  the `sampling` substream, at temperature τ (config, default 1).
- **Cache:** sqlite, keyed by *(backend, checkpoint id, placement, style,
  genome hash, observation text)*.

## A8. Teacher & mutation (local Ollama)

**Teacher model:** the largest local instruct model that generates at
≥ 15 tokens/s (typically 8–14B). Its prompt is `prompts/teacher_v1.md`,
containing:

- the world rules and action definitions;
- the genome, one line per locus;
- the situation text;
- this key instruction: ***"These instincts define the animal's
  personality. Follow them even when unwise. Instincts that are
  meaningless have no effect."***

Without that instruction, the teacher applies its own common sense and
ignores the genes, and so would the distilled student.

**Output modes:**

- **A "points":** a JSON schema with an integer 0–100 per action, then
  normalised and ε-smoothed. 1 call.
- **B "k-sample":** an enum schema at temperature 0.8, sampled with k = 8
  seeds to get an empirical distribution. k calls.
- **C "logprobs":** log-probabilities over the action token, only if the
  installed Ollama supports it (checked in S1).

The teacher gate (S4) picks the mode, based on directed accuracy, A-vs-B
agreement, and cost.

**Always, for every teacher call:**

- fixed seed and temperature;
- record the model **digest**;
- go through the sqlite cache;
- use `keep_alive` during batch jobs;
- set `OLLAMA_NUM_PARALLEL` to speed up labelling on GPU.

**Mutation operators.** Applied per locus, with `p_mut` = 0.03 per child
by default.

| Operator | Uses LLM | What it does |
|---|---|---|
| `llm_rewrite(style)` | yes, a small mutator model (1–4B) | Style is one of: random change, invert, exaggerate, soften, add condition, specialise, generalise. No fitness context is given. |
| `intensity` | no | never ↔ rarely ↔ sometimes ↔ often ↔ always |
| `negate` | no | toggles avoid/seek, always/never |
| `condition_swap` | no | *when hungry* ↔ *when full* ↔ *when threatened* ↔ *when alone* |
| `synonym` | no | small lexicon |
| `founder_reintroduce` | no | a random founder allele of the same locus |

**Guards:**

- word cap; a single sentence; English;
- strip meta-text and quotes;
- the result must differ from the parent;
- up to 3 retries, then fall back to a word operator.

Every mutation is cached and logged: parent, operator, style, seed, model
digest.

## A9. Metrics

Notation: `p(a|o,g)` is the backend's action distribution for observation
`o` and genome `g`. H is entropy in bits. JSD is the Jensen–Shannon
divergence in bits, between 0 and 1.

**Sensitivity metrics:**

- **Genome information:**
  `MI_G = mean_o [ H(mean_g p(·|o,g)) − mean_g H(p(·|o,g)) ]`.
  How much the genome determines the action, given the situation. The
  maximum is log₂7 ≈ 2.81 bits.
- **Observation information:**
  `MI_O = mean_g [ H(mean_o p) − mean_o H(p) ]`.
  This must stay high, or the agents ignore their world.
- **Directed test for action locus ℓ:** take `g_pro` and `g_anti`, which
  differ only at ℓ (all other loci neutral). Over the observations `O_ℓ`
  where ℓ is relevant:
  `ΔP_ℓ = mean_{o∈O_ℓ} [ p(ℓ|o,g_pro) − p(ℓ|o,g_anti) ]`.
  Report mean ΔP and sign accuracy over the (ℓ, o) cells.
- **Behaviour distance:**
  `d(g,g′) = mean_o JSD(p(·|o,g), p(·|o,g′))`.
- **Locality:** compare the distribution of d(parent, single-edit child)
  with d(random founder pairs). Also report Spearman ρ between text distance
  and d. Text distance is embedding cosine from a local embedding model, or
  normalised edit distance as a fallback.
- **Crossover blending:** d(child, nearer parent) compared with
  d(parent₁, parent₂).
- **Gibberish sensitivity:** mean d(random-text genome, neutral genome).
- **Teacher agreement:** mean KL(teacher ‖ student) on held-out data.

**Evolution metrics:**

- population, births, deaths by cause, lifespan;
- lineage depth (generations);
- allele frequencies per locus;
- per-locus Shannon diversity and allele count;
- mean gene length in words, to detect bloat;
- mutant establishment rate per operator (the fraction of mutant alleles
  that reach ≥ 5 % frequency).

**Common garden (E5):** each genome group runs alone in K = 10 fixed arenas,
with no reproduction, T = 1 500 ticks and N = 20 agents. Scores are mean
survival ticks and food eaten. Groups are compared with bootstrap
confidence intervals.

## A10. Experiments

| ID | What | Sizes (small / full) | Session |
|---|---|---|---|
| E0 | Probes: Laya & Ollama API facts, token budget | — | S1 |
| E1a | Input-format study, zero-shot: P1–P4 × V1–V2 | (10 founders + 14 contrast + 1 neutral + 10 random) × 48 obs × 8 formats | S3 |
| E1b | Zero-shot sensitivity on the best format | 30 founders, 30 random, 14 contrast, 1 neutral, 10 × 4 word-edits; × 48 / 150 obs | S3 |
| E2 | Throughput: checkpoint × device × #options × text length × batch | ~2 k calls | S3 |
| E3 | Teacher gate → dataset → distillation → re-run the E1 suite on held-out alleles + LLM mutants; compare zero-shot vs fine-tuned vs teacher | 3 k / 10 k labels | S4–S5 |
| E4 | Evolution matrix (below) | 20 k ticks per run; 3 / 5 seeds | S6 |
| E5 | Common garden (+ optional competition assay: evolved vs founders in one arena) | K = 10 arenas | S6–S7 |

**E4 conditions.** All use the same world seed; the simulation seeds vary.

| Condition | Definition |
|---|---|
| C1 FULL | fine-tuned Laya, sexual reproduction, mixed mutation, selection |
| C2 NO-MUT | `p_mut = 0`: selection on founder variation only |
| C3 SHUFFLED | each decision uses a random *other* living agent's genome, so genes are inherited but don't affect their carrier: drift only |
| C4 RANDOM-FOUNDERS | founders drawn from the control (random-text) alleles |
| C5 RULE-BASED | C1 with the `rule_based` backend (fast reference; runs on CPU in parallel) |
| C6* ZERO-SHOT | C1 with zero-shot Laya, only if E3 shows a large gap |
| C7* ASEXUAL | one parent, copy + mutation (the old lab's regime) |

## A11. Compute budget

These estimates come from published latencies. S1 and S3 replace them with
measured numbers in `STATUS.md`. Assumed: Laya ≈ 40 ms/call on GPU,
≈ 0.2–0.5 s on CPU; teacher (8–14B) ≈ 1.5–3 s per label on GPU,
≈ 10–20 s on CPU.

| Job | Calls | GPU | CPU only |
|---|---|---|---|
| E1a | ~13 k Laya | ~10 min | 1–2 h |
| E1b | 5.5 k / 30 k Laya | 4 / 20 min | 0.5 / 2.5 h |
| E2 | ~2 k Laya | minutes | ~15 min |
| Teacher gate | 1–2 k teacher | 15–45 min | 3–8 h |
| Teacher labels | 3 k / 10 k | 1.5–2.5 h / 4–8 h | 10–16 h / not advised |
| Fine-tune | 3–10 k examples × 3 epochs | 20–60 min | 1–3 h (head-only) |
| E3 eval | ~10 k Laya | ~10 min | ~1 h |
| E4, one run | ~200 k decisions, 5–20 % uncached | 10–30 min | 1–4 h |
| E4 matrix | 4 Laya conditions × 5 seeds, + rule-based in parallel | 4–10 h | 3 conditions × 3 seeds ≈ 1–2 days |

Without a GPU, teacher labelling and the E4 matrix are the bottlenecks. Use
the small profile, run them overnight, and keep to 3 seeds.

## A12. Risks & fallbacks

| Risk | Fallback |
|---|---|
| Laya returns only the top choice, not per-option probabilities | Use the internal logits found in S1. Otherwise use a confidence-weighted one-hot plus temperature (weaker; flag it). |
| Criteria text barely affects the scorer (P1 fails) | P2/P4 (genes in the state), or the multilingual checkpoint (bigger head budget). |
| Question segment exceeds `head_max_len` | Shorter alleles; fewer loci in criteria; the multilingual checkpoint. |
| No batch API | Sequential calls + cache. Optionally a hand-batched forward pass through the internals. |
| Teacher ignores genes | Stronger instruction, bigger model, or mode B. At most 3 iterations, then escalate to the owner. |
| Laya fine-tuning code unusable locally | Head-only training on a frozen encoder (layaMOE approach). Last resort: our own small option scorer over frozen Laya embeddings. |
| VRAM conflict between Laya and Ollama | Never run labelling and fine-tuning at the same time. Unload Ollama models (`keep_alive: 0` / `ollama stop`) before training. |
| Population dies out or explodes | Tune in S2.6 with `rule_based`, before any Laya run. |
| Too few generations in 20 k ticks | Shorten maturity and lifespan, or run longer on the best condition only. |

---

# Part B — Runbook

## B0. Context protocol

Six rules make multi-session work cheap:

1. **Files are memory; context is scratch.** Anything worth keeping goes
   into `STATUS.md`, `results/*.md` or code, *before* any `/compact` or
   `/clear`.
2. **Three levels of reset:**
   - **⏸ compact point**, within a session: after noisy or exploratory
     steps, before a large implementation step. Claude updates STATUS,
     commits, then **stops**. You type the given `/compact <focus>`.
   - **Session boundary:** Claude says ✅. You type `/clear` (or quit) and
     paste the kickoff prompt for the next session.
   - **Emergency:** if `/context` shows more than ~60 % used mid-step, say
     *"Write where you are into STATUS.md, then stop"*, then `/compact`.
3. **Output hygiene:**
   - Scripts print ≤ 20 lines; details go to files.
   - Use `pytest -q -x` and `| tail -n 30`.
   - Never `cat` data or logs; use `python -m experiments.peek`.
4. **Background jobs:** anything longer than ~2 minutes runs with `nohup`
   and writes a `logs/<job>.progress.json`.
   - Check jobs with `python -m experiments.status` (one line per job).
   - No polling loops. Quit the session and come back later.
   - You can also launch the printed command in your own terminal.
5. **Subagents for reading:** third-party code (the `laya` package, its
   notebook) is read by a subagent that returns ≤ 30 lines. The main
   context never sees the source.
6. **One step = one commit:** `spike S<n>.<step>: …`. `git log --oneline`
   becomes a cheap progress log.

**Your commands:**

| When | You type |
|---|---|
| Start any session | `cd prototype && claude`, then paste the **kickoff prompt** below |
| Claude says **⏸ safe to compact** | `/compact <focus text shown in that step>` |
| Claude says **✅ session complete** | `/clear` (or quit), then the kickoff prompt again later |
| Check context usage | `/context` |
| A job is still running | quit; next time, the kickoff prompt resumes from STATUS |

**Kickoff prompt.** The same prompt works for every session, and for
resuming after a crash:

```
Read STATUS.md. Then run `git log --oneline -8` and, if it exists,
`python -m experiments.status`. In ../Docs/redesign/08-phase0-spike-plan.md
read ONLY the "Revision" box at the top, section B0, the session section named in STATUS.md › Next action,
and the Part A sections that session lists under "Reads". Continue from
STATUS.md › Next action. Follow CLAUDE.md. Stop at every ⏸.
```

**Human checkpoints.** The owner decides at each of these (5–10 min each):

- **H1:** approve the founder pool (end of S1).
- **H2:** zero-shot results and input format (end of S3).
- **H3:** teacher prompt and mode (S4.3).
- **H4:** approve the preregistered E4 matrix (S6.4).
- **H5:** go / no-go (end of S7).

**Session map:**

| Session | Goal | Claude time | Background compute | Ends with |
|---|---|---|---|---|
| S1 | Scaffold, probes, founder pool | 1–1.5 h | — | H1 |
| S2 | World, sim, rule-based backend | ~2 h | minutes | stable smoke run |
| S3 | Laya backend, metrics, E1, E2 | 1.5–2 h | 0.5–3 h | H2 |
| S4 | Teacher gate, mutation, dataset, labels | ~2 h | 1.5–16 h | labelling job running |
| S5 | Distillation + E3 | ~2 h | 0.5–3 h | G1–G3 verdict |
| S6 | Evolution matrix + analysis code | ~2 h | 4 h – 2 days | H4, matrix running |
| S7 | Analysis, report, docs update | ~1.5 h | < 1 h | H5 |

Total: about 12 h of interactive Claude time, plus unattended compute.

---

## S1 — Scaffold, probes, founder pool

**Reads:** A3, A5, A7 (placements), A8 (output modes), A12.

**1.1 Scaffold**

- Create:
  - `pyproject.toml`, the package skeleton (A3), `configs/{base,small,full}.yaml`;
  - `promptevo/rng.py`: named substreams from `numpy.random.SeedSequence`;
  - `promptevo/cache.py`: sqlite KV store, sha256 key → JSON value;
  - `promptevo/progress.py`: atomic progress JSON;
  - `experiments/status.py`: one line per `logs/*.progress.json`;
  - `experiments/peek.py`: first N rows or summary of JSONL/CSV.
- Tests: cache round-trip; rng reproducibility and substream independence.
- Exit: `pytest -q` is green. Commit `spike S1.1`.

**1.2 Laya probe** (`experiments/e0_probe_laya.py`)

Use a **subagent** to read the installed `laya` package source and answer
in ≤ 25 lines. Then verify each answer with a tiny script.

1. Package, torch and device versions.
2. The `predict()` return schema: does a `choice` answer include **all
   per-option probabilities**? What are the key names?
3. Does `criteria` accept label → description, and **does the description
   text change the scores**? Test: vary only one description.
4. Tokenizer access. Our question segment's token count. `head_max_len`.
   What happens on overflow: error or silent truncation?
5. Is there a batch API (a list of states, or a `predict_batch`)? If not,
   is there a usable internal forward pass?
6. Determinism: the same input twice gives identical probabilities?
7. Latency of a single call for English and multilingual, 1 vs 7 options,
   short vs long state.
8. Is `ONNXAgent` available (optional)? Is the calibration temperature
   exposed?

Write `results/e0_laya_api.md` (≤ 60 lines) and copy the facts into
STATUS › *API facts — Laya*. Commit.

**1.3 Ollama probe** (`experiments/e0_probe_ollama.py`)

1. Ollama version, available models and their digests.
2. Structured output (`format` = JSON schema) works with the candidate
   teacher.
3. Is `seed` + `temperature 0` deterministic? Repeat 3 times.
4. Logprobs support.
5. Tokens/s for prompt evaluation and generation, for the teacher and
   mutator candidates.
6. Is an embedding model available?

Pick the teacher, mutator and embedding models and put them in
`configs/base.yaml`. Write `results/e0_ollama.md`, then the STATUS facts.
Commit.

⏸ **Compact A:**
`/compact Keep: step position, the Laya and Ollama API facts now in STATUS.md, chosen models, files created in S1. Drop: probe outputs, source excerpts, tracebacks.`

**1.4 Allele files**

- Write `data/founder_pool_v1.json`, `data/contrast_alleles_v1.json` and
  `data/control_alleles_v1.json`, following A5.
- Add `promptevo/founder.py` to load them and sample genomes.
- Tests: schema; word caps; exactly 1 neutral allele per locus.

**1.5 Token budget** (`experiments/e0_budget.py`)

- For 1 000 random founder genomes, and for worst-case maximum-length
  alleles, measure the P1/P4 question-segment tokens against
  `head_max_len`, for both checkpoints.
- If anything is over budget: shorten alleles, or switch the default to
  multilingual. Record the decision in STATUS › *Decisions*.

**Exit gate**

- Facts are recorded; the pool files pass tests and the budget check.
- STATUS › Next action = `S2.1`.
- Commit, then say ✅.
- **H1:** the owner reviews the founder pool (a readable table in
  `results/founder_pool_v1.md`).

---

## S2 — World, simulation, rule-based backend

**Reads:** A4, A5 (crossover), A6, A7 (`rule_based`).

**2.1 Genome** (`genome.py`)

- Allele registry, `Genome` tuple, hashing, uniform crossover.
- Tests: crossover takes each locus from one parent; the hash is stable.

**2.2 World** (`world.py`, `render.py`)

- Terrain generation with the connectivity check; food regrowth;
  predators; greedy 8-neighbour steering that respects blocked cells;
  the ASCII renderer.
- Tests: over 1 000 ticks no agent or predator enters a blocked cell;
  same seed → same world.

⏸ **Compact B:**
`/compact Keep: step position, public APIs of genome.py and world.py (function names + signatures), test status. Drop: file bodies, test output.`

**2.3 Perception, text and actions**

- `perception.py` (`Observation` dataclass, thresholds from config),
  `obs_text.py` (V1/V2), `actions.py` (7 executors, energy costs).
- Tests: hand-built scenarios, e.g. flee increases distance to the
  predator; eat on food adds energy; invalid choice → wander.

**2.4 Backends**

- `backends/base.py` (protocol), `random_policy.py`, `rule_based.py`.
- Tests: an *"always flee"* gene raises P(flee) when a predator is near;
  gibberish equals neutral.

**2.5 Simulation loop** (`sim.py`, `eventlog.py`)

- Lockstep loop: decision every D ticks → cache lookup → batch the
  uncached queries to the backend → sample → execute D ticks.
- Metabolism; deaths (starvation / predator / age); mating; floor;
  population cap.
- Logs: `events.jsonl`, `stats.csv`, the allele registry, and progress.
- **Determinism test:** two runs with the same seed produce the same
  sha256 of `events.jsonl`.

**2.6 Smoke run and tuning**

- 5 k ticks, `rule_based`, small profile.
- Show 3 ASCII snapshots and a ≤ 20-line summary.
- Tune the A4 parameters until the population is stable at 20–40 with
  turnover, mean lifespan is 300–800 ticks, and there are ≥ 10
  generations per 20 k ticks.
- Record the final values in `configs/base.yaml` and STATUS.

**Exit gate**

- Tests green; the smoke run is stable.
- STATUS › Next action = `S3.1`.
- Commit, then say ✅.

---

## S3 — Laya backend, metrics, zero-shot sensitivity, benchmark

**Reads:** A6 (text styles), A7, A9, A10 (E1a, E1b, E2), A12, and
STATUS › *API facts — Laya*.

**3.1 Laya backend** (`backends/laya_backend.py`)

- Placements P1–P4 × styles V1/V2.
- Map outputs to the fixed action order.
- Cache.
- Use the batch path if S1 found one; otherwise sequential.
- Token-budget guard: raise in tests, warn in runs.
- Tests use a fake Laya (monkeypatch), plus 1 real smoke call marked
  `@pytest.mark.laya`.

**3.2 Metrics** (`metrics.py`)

- Every A9 sensitivity metric.
- Tests on synthetic distributions with known answers, e.g. identical
  genomes → MI_G = 0.

⏸ **Compact C:**
`/compact Keep: step position, laya_backend and metrics public APIs, test status, any Laya quirks found. Drop: code bodies, test output.`

**3.3 Observation set** (`experiments/make_obs.py`)

- ~32 synthetic key situations, plus observations stratified-sampled from
  a rule-based run, up to 48 (small) / 150 (full).
- Tag each observation with the directed tests it serves
  (`food_present` → eat, `predator_near` → flee, …).
- Output: `data/observations_v1.jsonl`.

**3.4 E1a: input-format study**

- Run as a **background job**.
- Then write `results/e1a_formats.md`: one row per format with MI_G
  (founders), MI_G (random), directed accuracy, mean ΔP, MI_O.
- Put the best (placement, style) into config.

**3.5 E1b: zero-shot sensitivity**

- On the best format, as a background job.
- Single edits use **word operators only**; LLM mutants arrive in S4.

**3.6 E2: benchmark**

- Run *after* E1, so timings aren't disturbed.
- Measure p50/p95 latency and throughput per factor.
- Write `results/e2_bench.md` and update the A11 numbers in STATUS.

**3.7 Report** (`experiments/report.py e1`)

- Write `results/e1_zero_shot.md` (≤ 80 lines), with the key numbers in
  STATUS.

> **Shortcut:** if zero-shot Laya already passes G1–G3, skip S4–S5 and go
> straight to S6 with zero-shot Laya. Record this in STATUS › *Decisions*.

**Exit gate**

- STATUS › Next action = `S4.1`.
- Commit, then say ✅.
- **H2:** the owner reads `e1_zero_shot.md` and `e1a_formats.md`.

---

## S4 — Teacher gate, mutation operators, dataset, labelling

**Reads:** A8, A9 (directed test, MI), A10 (E3), and STATUS ›
*API facts — Ollama*.

**4.1 Ollama client** (`llm/ollama_client.py`)

- JSON-schema format, options (seed, temperature, num_ctx), `keep_alive`,
  retries with backoff.
- Cache keyed by (model digest, prompt hash, options).
- Modes A / B / C.
- Tests use a fake HTTP server.

**4.2 Teacher prompt and policy**

- `prompts/teacher_v1.md` and `backends/ollama_policy.py`. The policy
  reuses the E1 metric code unchanged.

**4.3 Teacher gate** (`experiments/teacher_gate.py`)

- Contrast genomes + 10 founders + 10 random, × 24 observations, for
  modes A and B (and C if available).
- **Must pass** directed accuracy ≥ 85 %, and MI_G (founders) clearly
  greater than MI_G (random).
- Otherwise iterate on the prompt or the model, at most 3 times. Log each
  iteration in STATUS.
- **H3:** the owner reviews the prompt, the mode and the numbers.

⏸ **Compact D:**
`/compact Keep: step position, chosen teacher model+mode+prompt version and its gate numbers, ollama_client API. Drop: prompt iterations, raw teacher outputs.`

**4.4 Mutation** (`evolution/mutation.py`, `prompts/mutate_v1.md`)

- All A8 operators and guards, plus the cache and log.
- Tests use a fake LLM.
- Then `experiments/make_mutants.py`: about 6 mutants per founder allele
  (mixed operators), plus ~50 fully novel LLM-written alleles as an
  out-of-distribution (OOD) set. Output: `data/mutants_v1.jsonl`.

**4.5 Dataset** (`experiments/make_dataset.py`)

- **Allele split per locus:**
  - founder alleles → train;
  - mutants of one founder allele → validation;
  - mutants of another founder allele, plus the OOD set → test.
- **Genomes:**
  - random combinations;
  - **contrast groups**: one base genome × 3 variants that differ at
    exactly one locus, all on the *same* observations;
  - ~10 % random-text genomes, to teach "gibberish ≈ neutral".
- Size: 3 k (small) / 10 k (full).
- Write the keys only: `data/dataset_v1_{train,val,test}.jsonl`.

**4.6 Labelling** (`experiments/label_teacher.py`)

- Resumable: append to JSONL and skip keys already done.
- Launch it in the **background**.
- Check the first 50 labels: not all uniform, sums valid, contrast groups
  differ.

**Exit gate**

- The labelling job is running or done, and STATUS › *Background jobs*
  lists its log path, progress file and ETA.
- STATUS › Next action = `S5.1` (wait until the train split is labelled).
- Commit, then say ✅.

---

## S5 — Distil the teacher into Laya, then evaluate (E3)

**Reads:** A9, A10 (E3), A12 (fine-tuning fallbacks), and STATUS ›
*API facts — Laya*.

**5.1 Fine-tuning facts (subagent)**

- A subagent reads the fine-tuning code in the `laya` package and repo
  (notebook, README section) and returns ≤ 30 lines: entry points, data
  format, loss, trainable modules, temperature fitting, save/load.
- Paste the result into STATUS › *FT facts*.

**5.2 Choose the path**

- **A:** full fine-tune, if VRAM ≥ ~16 GB.
- **B:** head-only on a frozen encoder; works on CPU.
- **C:** our own scorer (last resort).
- Before GPU training, unload the Ollama models.

**5.3 Training** (`experiments/finetune_laya.py`)

- Convert dataset rows to Laya inputs using the chosen placement.
- Loss: KL(teacher ‖ student) on soft labels.
- Early stopping on validation KL. Save to `models/laya-evo-v1`.
- Fit the calibration temperature on the validation set.
- Run in the **background**.

⏸ **Compact E:**
`/compact Keep: step position, FT path chosen, training command, checkpoint path, val metrics. Drop: training logs, library excerpts.`

**5.4 E3 evaluation** (`experiments/e3_eval.py`)

- Rerun the full A9 suite on the **test split** (held-out alleles + OOD),
  including LLM-mutant locality and crossover blending.
- Output one table: zero-shot vs fine-tuned vs teacher. Columns: MI_G,
  MI_O, directed accuracy, ΔP, locality ρ, gibberish distance, KL to the
  teacher, latency.

**5.5 Verdict**

- Record G1–G3 pass/fail in STATUS.
- If they fail, climb the A12 ladder, at most 2 iterations: more data,
  multilingual, a different placement, full fine-tuning instead of
  head-only. Then escalate to the owner.

**Exit gate**

- `results/e3_distill.md` is written.
- STATUS › Next action = `S6.1` (or blocked on the owner).
- Commit, then say ✅.

---

## S6 — Evolution experiments

**Reads:** A2 (G4, G5), A8 (mutation), A9 (evolution metrics, common
garden), A10 (E4, E5).

**6.1 Wire up evolution**

- Mutation inside reproduction (`p_mut`, operator weights from config).
- Lineage registry.
- Flags: `shuffled`, `random_founders`, `asexual`.
- Tests: with `shuffled`, the carrier's own genome is never used for its
  decisions; with `p_mut = 0`, no new alleles appear.

**6.2 Common garden** (`experiments/common_garden.py`)

- Fixed arena seeds.
- Input: genome groups (founders sampled at t = 0, and a sample of living
  genomes from the last 20 % of a run).

**6.3 Pilots**

- Rule-based C5 for 20 k ticks. Check the generation count and that
  allele dynamics are non-trivial.
- Fine-tuned-Laya C1 for 2 k ticks. Measure effective decisions/s and the
  cache hit rate, then **extrapolate the matrix runtime**. Pick the
  profile and seed count (G5 data point).

⏸ **Compact F:**
`/compact Keep: step position, pilot numbers (dec/s, cache hit rate, generations per 20k ticks), chosen profile/seeds, run_matrix plan. Drop: pilot logs.`

**6.4 Preregister and launch**

- Write the exact matrix, the metrics and the G4 test into STATUS ›
  *Preregistration*. **H4:** owner approval. No changes after that.
- Launch `experiments/run_matrix.py` in the background:
  - a queue with resume;
  - Laya runs one at a time on the GPU;
  - rule-based runs in parallel on CPU cores.

**6.5 Analysis code** (`experiments/analyze.py`), written while the
matrix runs

- allele-frequency trajectories (top 5 per locus);
- diversity and allele counts;
- bloat;
- establishment rate per mutation operator;
- the lineage text chain of the winning alleles ("which prompts survived");
- common-garden comparison with bootstrap CIs.
- Test it on the finished rule-based runs.

**Exit gate**

- The matrix is running and analysis is tested on real outputs.
- STATUS › *Background jobs* is filled in.
- STATUS › Next action = `S7.1`.
- Commit, then say ✅.

---

## S7 — Analysis, report, decision

**Reads:** A2, A9, A10, and STATUS (all sections).

**7.1 Report**

- Run `experiments/status.py` to confirm every run finished.
- Run `analyze.py` and `common_garden.py` on all runs.
- Write `results/REPORT.md` (≤ 200 lines), the PNGs, and
  `results/top_genes.md`: the surviving prompts per locus, with their
  mutation chains.

**7.2 Go / no-go**

- A G1–G5 table with numbers.
- Pick the branch from the A2 decision table.
- Recommendations: backend mode (A/B), checkpoint, population size,
  decision rate, hardware, founder-pool changes, mutation operator weights.
- **H5:** owner decision.

**7.3 Feed back into the design docs**

- Update [02](02-assessment.md), [05](05-decision-backend.md) and
  [07](07-open-questions-and-roadmap.md) with short findings that link to
  `REPORT.md`.
- List the pieces that carry over to Unity: founder pool, observation text
  format, teacher prompt, fine-tuned checkpoint, metrics.
- Commit and push, then say ✅.

**Optional S8: option B spike.** "Development at birth": one Laya call per
birth with several typed questions → phenotype parameters. Compare it with
C1 in the common garden at equal compute.
