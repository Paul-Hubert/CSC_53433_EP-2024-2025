# Glossary

Terms used across `Docs/system/`, alphabetical. Page numbers point to the file that defines them.

| Term | Meaning |
|---|---|
| **Action locus** | One of the 7 loci named after an action (`eat` … `attack`); its gene only describes that action. [05](05-genome.md) |
| **ACTIONS** | The tuple `(eat, flee, follow, wander, rest, mate, attack)`; fixes the column order of every probability vector. [04](04-agents-perception-actions.md) |
| **Allele** | One concrete gene text at one locus, with id `locus:n`, origin and lineage metadata. [05](05-genome.md) |
| **Allele registry** | Per-run store of all alleles; dedupes by `(locus, text)` and keeps the first origin. [05](05-genome.md) |
| **Asexual mode** | `evolution.sexual: false`: one parent clones itself plus mutation (control C7, the old lab's regime). [06](06-evolution.md) |
| **Backend** | Anything with `decide(queries) → probs[N, 7]`: `random`, `rule_based`, `llm`, `laya`. [07](07-decision-backends.md) |
| **Behaviour distance** | `d(g, g′) = mean_o JSD(p(·\|o,g), p(·\|o,g′))`. [09](09-metrics-and-experiments.md) |
| **Cap** | `agents.cap`: population size at which births are blocked (60; small 40). [03](03-simulation-loop.md) |
| **Chebyshev distance** | `max(\|dy\|, \|dx\|)`; the grid's range metric. [02](02-world.md) |
| **Common garden** | Evaluation where each genome group lives alone in fixed arenas without reproduction, scored by survival and food (E5, not built). [09](09-metrics-and-experiments.md) |
| **Contrast pair** | Two genomes, neutral everywhere except one action locus (pro: "Always …" vs anti: "Never …"); used for directed tests only. [05](05-genome.md) |
| **Control alleles** | 40 random-text sentences (20 shuffled words, 20 irrelevant sentences) for C4 and gibberish tests. [05](05-genome.md) |
| **Controls C1–C7** | The E4 conditions: full, no-mutation, shuffled, random founders, rule-based, zero-shot (dropped), asexual. [06](06-evolution.md) |
| **Decision period (D)** | `sim.decision_period` = 4 ticks between collective decisions. [03](03-simulation-loop.md) |
| **Digest** | Model hash reported by Ollama (`/api/tags`, 12 chars); part of every LLM cache key; `unknown` if not listed. [07](07-decision-backends.md) |
| **Directed test / ΔP** | Over relevant observations, `ΔP = p(ℓ\|o,g_pro) − p(ℓ\|o,g_anti)`; reported as mean ΔP and sign accuracy. [09](09-metrics-and-experiments.md) |
| **events_sha** | First 16 hex chars of the running sha256 over all event-log lines; equal digests = identical runs. [08](08-reproducibility-and-data.md) |
| **Floor** | `agents.floor` = 10: below it, immigrants from the founder pool are spawned. [06](06-evolution.md) |
| **Founder pool** | Fixed, versioned set of instinct-like alleles (4 + neutral per locus) from which every run starts; v1 is a draft (H1). [05](05-genome.md) |
| **Gates G1–G5** | Preregistered go/no-go criteria: semantics, information, locality, evolution, throughput. [09](09-metrics-and-experiments.md) |
| **Genome** | Immutable tuple of 10 allele ids, one per locus in `LOCI` order. [05](05-genome.md) |
| **Genome key** | First 24 hex chars of sha256 over the 10 gene texts; run-independent; used by memo and caches. [05](05-genome.md) |
| **Gibberish sensitivity** | Mean behaviour distance between random-text genomes and the neutral genome; should be small. [09](09-metrics-and-experiments.md) |
| **H1–H5** | Human checkpoints in plan 08 where the owner decides (H1 founder pool, H2 E1 results, H3 gate/prompt, H4 preregistration, H5 go/no-go). |
| **Immigrant** | Agent spawned by the floor rule; generation 0, logged as `immigrant`. [03](03-simulation-loop.md) |
| **Implicit selection** | No fitness function; alleles spread only through survival and successful mating. [06](06-evolution.md) |
| **Invalid action** | Chosen action without a target (e.g. `eat` with no food in view); executed as `wander` and counted. [04](04-agents-perception-actions.md) |
| **JSD** | Jensen–Shannon divergence in bits (0…1). [09](09-metrics-and-experiments.md) |
| **ksample mode** | LLM mode: k = 8 sampled single-action answers at temperature 0.8, add-one smoothed; for checks only. [07](07-decision-backends.md) |
| **Locality** | Ratio of median single-edit behaviour distance to median unrelated-founder distance (G3: ≤ 0.5). [09](09-metrics-and-experiments.md) |
| **Lockstep** | The tick waits for all decisions; slow backends slow the run but do not change results. [03](03-simulation-loop.md) |
| **Locus** | A fixed gene slot with a semantic role; 10 in total, fixed order. [05](05-genome.md) |
| **logprobs mode** | LLM mode: one generated token; the distribution comes from Ollama's top-10 token log-probabilities; falls back to points (unverified). [07](07-decision-backends.md) |
| **mate_ready** | `age ≥ 150` and `energy ≥ 50`. [04](04-agents-perception-actions.md) |
| **Memo** | Per-run dict `(genome_key, Observation) → probs`; each pair is asked once per run. [03](03-simulation-loop.md) |
| **MI_G** | Mutual information between genome and action given the situation: `mean_o [H(mean_g p) − mean_g H(p)]` (G2: ≥ 0.25 bits). [09](09-metrics-and-experiments.md) |
| **MI_O** | Mutual information between situation and action per genome: `mean_g [H(mean_o p) − mean_o H(p)]` (G2: ≥ 0.25 bits). [09](09-metrics-and-experiments.md) |
| **Mutator** | (1) `evolution/mutation.Mutator`, applying operators per locus at `p_mut`; (2) the small LLM (`ollama.mutator_model`) used by `llm_rewrite`. [06](06-evolution.md) |
| **Neutral allele** | "No preference." (actions) / "No particular temperament." (temperament); part of the founder options so evolution can switch a drive off. [05](05-genome.md) |
| **Newcomer** | Agent with no action yet (just born or immigrated); decides on the next tick even between decision ticks. [03](03-simulation-loop.md) |
| **Observation** | Frozen, discretised situation: energy, food, predator, animal (+ ready, stronger), age. [04](04-agents-perception-actions.md) |
| **Ollama** | Model server used for the LLM brain and mutation, local (`localhost:11434`) or cloud (`ollama.com`, key in `OLLAMA_API_KEY`). [07](07-decision-backends.md) |
| **Option A / option B** | A: the model decides every (uncached) action ("LLM as brain", implemented). B: the model runs once at birth and outputs a phenotype ("LLM as development", not built). [07](07-decision-backends.md) |
| **Origin** | Allele provenance: founder, neutral, contrast, control, mutant, ood. [05](05-genome.md) |
| **p_mut** | Mutation probability per locus per child (0.03). [06](06-evolution.md) |
| **Placement (P1–P4)** | Where genes go in Laya's input (parked). [07](07-decision-backends.md) |
| **points mode** | Default LLM mode: JSON with 0–100 points per action, normalised. [07](07-decision-backends.md) |
| **Prefetch** | In table mode, filling a call up to K situations with the run's most frequent uncached situations for that genome. [07](07-decision-backends.md) |
| **Profile** | Config overlay: `small` (default, CPU) or `full` (= base). [10](10-configuration-reference.md) |
| **Query** | `(genome_key, genes, obs)` sent to a backend. [07](07-decision-backends.md) |
| **RELEVANT_TAG** | Map from action locus to the observation tag that makes its directed test meaningful. [04](04-agents-perception-actions.md) |
| **Rule-based backend** | Keyword "ideal interpreter" of genes; fast reference and control C5. [07](07-decision-backends.md) |
| **Stream** | Named, independently seeded random generator (`world`, `predators`, `agents`, `sampling`, `actions`, `mutation`, `food`). [08](08-reproducibility-and-data.md) |
| **table mode** | LLM mode: one call answers up to K = 8 situations for one genome; fewer requests, more generated answers. [07](07-decision-backends.md) |
| **Teacher** | The LLM decision model under its old name: in the parked distillation plan it labelled data for Laya; `teacher_v1.md`, `teacher_gate.py`, `TeacherBackend` keep the name. [07](07-decision-backends.md) |
| **Temperament locus** | `risk`, `social`, `place`: general dispositions that may affect several actions. [05](05-genome.md) |
| **Text style (V1 / V2)** | Observation wording: terse key–value (V1) or first-person sentences (V2). [04](04-agents-perception-actions.md) |
| **Uniform crossover** | Each locus taken from either parent with probability ½. [06](06-evolution.md) |
| **World seed / sim seed** | World seed fixes terrain and initial food; sim seed fixes everything else. [08](08-reproducibility-and-data.md) |
