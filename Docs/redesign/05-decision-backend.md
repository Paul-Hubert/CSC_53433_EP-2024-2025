# 05 — Decision backend: Laya (local) + Ollama Cloud (generation)

> Updated 2026-09-27: "J-Laya" = **Jev and Laya**, two *decision models*.
> Laya is the chosen local decision backend. Generative work (mutation,
> founder drafting, teacher labels) goes to a pooled **Ollama Cloud**
> subscription, with local Ollama as fallback.

Sources: [Laya model card](https://huggingface.co/convaiinnovations/laya),
[Laya GitHub](https://github.com/NandhaKishorM/laya),
[laya-typed-decisions](https://huggingface.co/convaiinnovations/laya-typed-decisions),
[Mysore, "What Is Laya? Laya vs Jev, with Live Demo" (Medium, Sept 2026)](https://medium.com/@visrow/what-is-laya-laya-vs-jev-with-live-demo-42c2ab494e02),
[layaMOE](https://github.com/vishalmysore/layaMOE),
[layaForWeb](https://github.com/vishalmysore/layaForWeb),
[ollama.com/pricing](https://ollama.com/pricing).
Numbers below are as published by those sources — **re-measure on our hardware**.

## 1. What a decision model is

A generative LLM answers "what should the animal do?" by *writing text*
that must then be parsed. A **decision model** instead takes:

- a **state** (text or JSON), and
- a set of **typed questions**, each with options defined *at request time*:
  - `choice` — one label from a list (e.g. `eat | flee | follow | …`)
  - `score` — a level on an ordered scale (e.g. speed 1–3), with per-level
    probabilities and an expected value
  - `noul` — a yes/no statement, returned as P(yes)

…and returns **one calibrated probability distribution per question**, all
questions in **one forward pass**. No generation loop, no parsing, no
invalid answers. This is exactly the "type-safe action decider" the vision
calls for.

## 2. Jev vs Laya

| | **Laya** (Convai Innovations) | **Jev** (TypeSafe AI) |
|---|---|---|
| Access | Open weights, Apache-2.0, `pip install laya` | Closed, hosted API, early access |
| Runs | Locally: GPU, CPU, even browser (ONNX Runtime Web, int8) | Cloud only |
| Architecture | ModernBERT-large encoder (395M) + decision head = 421M. Multilingual variant on mmBERT-base, 322M | Not disclosed |
| Latency | ~40 ms per call on a T4 (English), ~33 ms multilingual; **193–464 ms on CPU** | p50 ≈ 236–276 ms per call (third-party benchmarks) |
| Cost | $0 (own hardware) | ≈ $0.042 / M input tokens |
| Context | 512 tokens (English); 1 024, up to 8 192 (multilingual) | Longer |
| Many options | Weak: 77 labels → 0.425 on Banking77. **Keep ≤ ~20 options per question** | Strong (0.870 on Banking77) |
| Zero-shot | **Near random on its own typed-decisions benchmark (0.362 vs 0.318 random)**; 0.766 after fine-tuning | Better out of the box |
| Calibration | Over-confident as shipped; needs a per-(type, #options) temperature fit | Better calibrated out of the box |
| API | `laya-serve` exposes the **same `POST /v1/systemone` shape as Jev** | `POST /v1/systemone` |

**Decision:** Laya for the simulation (local, free, fast, open, fine-tunable,
and the model itself becomes teaching material). Jev is at most an optional
comparison baseline. Because the APIs match, one client class covers both.

## 3. Consequences for our design (important)

1. **Laya must be specialised to be useful.** Zero-shot it is close to
   random on decision benchmarks. This confirms risk R1 in
   [02](02-assessment.md): out of the box, gene text would barely drive
   behaviour. → We **fine-tune** (see §5). This turns the biggest risk
   into a course feature.
2. **Short context.** 512 tokens for the English model, and the question +
   options share a smaller budget (`head_max_len` = 192 English / 256
   multilingual). Genes must be **short** (≈ 8–15 words), and the genome +
   observation must fit. The multilingual checkpoint (1 024) gives headroom
   and is ~2× faster — likely the better default; to be measured.
3. **No prefix caching.** It is a bidirectional encoder: the whole input is
   re-encoded every call. The KV-cache trick from the LLM plan does not
   apply. What still works: **batching agents**, **response caching** on
   discretised observations, and slower decisions (0.5–2 Hz sim time).
4. **Several questions per pass for free.** One call can return action,
   direction, speed and flags together.
5. **Calibrated probabilities → sampled behaviour.** We sample actions from
   the returned distribution with the sim's seeded RNG. This gives graded,
   heritable variation instead of all-or-nothing switches. Temperature
   becomes a tunable parameter (or even an evolvable gene).
6. **≤ ~20 options per `choice`.** Our action set (≈ 8–10) and direction set
   (≈ 8) fit comfortably.

## 4. Where the genes go: mapping the genome onto Laya's input

Laya's input has three text slots: the **state**, each question's
**instructions**, and each option's **criteria** (label + description).
Genes can live in any of them. Proposal: use two gene families.

- **Action genes (loci = action options).** Each option in the action
  question carries a gene as its criteria text:

  ```json
  "action": {
    "type": "choice",
    "instructions": "What does this animal do next?",
    "criteria": {
      "eat":    "<gene L_eat>     e.g. 'eat whenever food is within reach'",
      "flee":   "<gene L_flee>    e.g. 'run when anything bigger comes close'",
      "follow": "<gene L_follow>  e.g. 'stay near animals of my kind'",
      "attack": "<gene L_attack>  e.g. 'fight only when starving'",
      "mate":   "<gene L_mate>    e.g. 'seek a partner when energy is high'",
      "wander": "<gene L_wander>  e.g. 'explore new ground when safe'",
      "rest":   "<gene L_rest>    e.g. 'stop moving when tired'"
    }
  }
  ```
  Loci are **homologous by construction** (the flee gene is always about
  fleeing), so per-locus crossover is meaningful (see [04](04-genome-and-evolution.md)).
  They must stay short because they share the question budget.

- **Temperament genes (in the state).** A few free-form genes are prepended
  to the observation, e.g. `Nature: cautious; prefers open ground; ...`,
  followed by the discretised observation text.

- **Other questions** (direction `choice`, speed `score`, `noul` flags such
  as "Is there danger?") use fixed wording at first. Students can make
  them evolvable later.

Which placement makes genes actually change decisions is an **empirical
question**. It is the first thing the Phase 0 spike measures.

## 5. Fine-tuning Laya to read genes: distillation from a large LLM

Idea: use a **large cloud LLM as the teacher** and Laya as the fast
**student**.

1. Generate many `(genome, observation)` pairs: founder alleles + random
   mutations × procedurally generated situations.
2. Ask the teacher LLM (Ollama Cloud) what an animal with *that* genome
   would do, as probabilities over the same typed options.
3. **Include contrast sets:** the *same* observation with different genomes
   must receive different labels. This is what forces Laya to read the
   genes instead of ignoring them (R1).
4. Fine-tune and fit calibration temperatures. Options:
   - full fine-tune with the official Kaggle notebook (free 2× T4), or
   - a cheaper **expert head** as in layaMOE (frozen shared encoder,
     ~26.5 M-param head, trained on a laptop CPU in < 2 h per the author).
5. Validate on held-out genomes: gene-sensitivity and agreement with the
   teacher.

Pedagogical bonus: **"train your species' instinct head"** can be a
student lab (dataset design → fine-tune → calibration → evaluation).
Caveat from the layaMOE write-up: a model trained on synthetic rules learns
exactly those rules. Teacher prompt diversity matters.

## 6. Runtime integration with Unity

- **v1 (recommended):** Python sidecar running Laya (`laya-serve`, or a thin
  FastAPI wrapper if we need **cross-agent batching**; to verify whether
  `/v1/systemone` accepts batches). Unity sends one batched request per
  decision step over localhost HTTP. The same client can target Jev or a
  shared lab server by changing the base URL.
- **v2 (experiment):** run the ONNX export **inside Unity** (Unity Inference
  Engine / Sentis) → no Python for students. Needs checking: ModernBERT op
  coverage, tokenizer in C# (layaForWeb already reimplemented tokenizer +
  sequence building in JS and checked it token-for-token against Python,
  so the pieces exist), and CPU speed.
- **Fallback:** `RuleBasedBackend` (no model) for CI and machines that
  can't run Laya.

## 7. Performance budget (Laya, per-decision mode)

| Setup | Latency / call | 50 agents @ 1 Hz, unbatched |
|---|---|---|
| T4-class GPU | ~33–40 ms | ~2 s per sim-second → batching needed for real time |
| Laptop CPU | ~193–464 ms | ~10–23 s per sim-second → **too slow; use smaller pop., lower Hz, cache, or option B** |

With lockstep simulation this is "slow", not "broken": results stay
identical, the run just takes longer. Plan to measure batched throughput
on (1) student laptop CPU, (2) a mid-range GPU, (3) a shared server.
Option B ("development at birth", see [02](02-assessment.md)) remains the
CPU-friendly path: one Laya call per birth, asking many typed questions
("How cautious is this animal?" `score`, "Preferred food distance?"
`choice`, …) whose answers become the agent's utility parameters.

## 8. Generative LLM: pooled Ollama Cloud subscription

Laya never generates text, so anything that *writes* text goes to a
generative LLM:

| Job | Volume | Where |
|---|---|---|
| Gene **mutation** (LLM rewrite operator) | Low: ≈ births × loci × p_mut. Hundreds per run | Ollama Cloud, cached course-wide |
| **Founder pool** drafting | One-off, then frozen | Ollama Cloud + human review |
| **Teacher labels** for fine-tuning Laya | One-off, large (tens of thousands of calls) | Ollama Cloud, instructor-run, batched |
| Run analysis ("summarise what this lineage evolved") | Low | Ollama Cloud |
| Offline / no-network fallback | — | Local Ollama with a small model |

Notes:
- Ollama Cloud uses the **same CLI and OpenAI-compatible API** as local
  Ollama, so switching between them is just a base URL and an API key.
- Plans (to verify at ollama.com/pricing, since quotas have changed several
  times): Free tier, Pro ≈ $20/mo, Max ≈ $100/mo, and a **Team plan with
  shared credits and unlimited users**, which looks like the right shape
  for pooling a class. Usage is metered against included credits, with
  session limits that reset every 5 hours and weekly limits. Budget the
  teacher-label job so it doesn't burn the class's weekly quota.
- **Reproducibility:** cloud models get updated. Log the model name and
  version with every mutation, and keep a **course-wide mutation cache**
  keyed by `(gene, operator, seed, model)` so reruns are identical and
  cheap.
- Put the API key in the sidecar/server config, **never** in the Unity
  project or the repo.
