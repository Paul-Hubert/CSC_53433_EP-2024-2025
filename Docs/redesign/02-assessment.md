# 02 — Honest assessment

**Short version:** the idea is good — genuinely novel for a teaching lab,
very readable for students, and close to active research (LLM-driven
evolution). But as literally described (*every agent queries an LLM every
decision*) it has three risks that can sink it: **compute throughput**,
**weak or chaotic mapping from gene text to behaviour**, and **LLM mutation
collapsing diversity**. All three are fixable by design choices made *now*,
and the whole concept should be validated in a cheap headless prototype
before any Unity work.

---

## What is strong

1. **Readable genomes.** The single biggest upgrade over the NN version.
   Students can literally read what evolved, diff two lineages, and argue
   about why *"flee only when energy is high"* beat *"always flee"*.
2. **Real agentic AI content.** Structured/typed LLM output, local
   inference, prompt design, LLM-as-operator — all current, all
   transferable skills.
3. **Maps to real research.** Students can be pointed at:
   - *Evolution through Large Models* (ELM), Lehman et al., 2022 —
     LLMs as mutation operators.
   - *Promptbreeder*, Fernando et al., 2023 — self-referential evolution of
     prompts.
   - *EvoPrompt*, Guo et al., 2023 — GA/DE over prompts with LLM operators.
   - *Generative Agents*, Park et al., 2023 — LLM-driven agents in a
     simulated world.
   Putting these in an ecology with implicit selection is a fresh twist.
4. **Common origin (fixed founder pool)** is the right call: runs become
   comparable, and "same start, different outcomes" is itself a lesson
   about contingency in evolution.
5. **Natural extension points** for students: new genes, new actions, new
   senses, new operators, new species, new terrain constraints.

## Risks (ordered by how likely they are to kill the project)

### R1 — Gene text may barely affect behaviour, or affect it chaotically
Evolution needs **heritable, graded variation**: small genotype changes
should *usually* cause small behaviour changes. Two failure modes:
- **Insensitivity.** Small models (and especially zero-shot classifiers)
  largely ignore instructions and key on the observation. Then all genomes
  behave the same and selection acts on noise. *This is the #1 risk if the
  backend is a classifier rather than an instruction-following LLM.*
- **Chaos.** One word changes the output distribution completely. Then
  offspring don't resemble parents and nothing accumulates.

**Mitigation:** measure it before building anything — a *gene-sensitivity
test* (see [07](07-open-questions-and-roadmap.md)): same observations,
different genomes → do action distributions differ, and do they differ
*smoothly* under small edits? Also: use **action probabilities** (logits
over the typed action set) and sample from them, rather than a single
greedy token — this makes behaviour graded instead of all-or-nothing.

### R2 — Compute: N agents × decisions/second × prompt length
A 20-gene genome is ~400–1 000 tokens. 100 agents deciding once per
second = 100 prompts/s. Re-processing the full prompt each time is
infeasible on a student laptop.

**Mitigations (combine them):**
- **Prefix caching.** The genome is constant for an agent's whole life —
  put it first, cache its KV state once per agent, and only process the
  short observation suffix (~30–60 tokens) per decision. (llama.cpp
  supports per-slot prompt caching.)
- **One forward pass per decision.** Constrain output to a single token from
  the action enum (or score action labels). No free-text generation at
  decision time.
- **Decisions are high-level and slow** (≈0.5–2 Hz). Between decisions a
  deterministic steering/locomotion layer executes the chosen behaviour
  ("flee from X" runs for 1 s). The LLM never does motor control.
- **Simulation time decoupled from wall-clock.** The sim ticks in lockstep
  and *waits* for decisions. Slower machine ⇒ slower run, identical results.
- **Smaller populations** (20–60) than the NN version.
- **Strong alternative — "development at birth":** see below.

### R3 — LLM mutation without context drifts toward bland text
Asking a small LLM "make a random change" repeatedly tends to produce
**regression to the mean**: genes become generic, longer, and more alike
("The animal should carefully balance…"). Diversity collapses, and length
bloats (the prompt-evolution equivalent of *code bloat* in GP).

**Mitigations:**
- A **menu of mutation operators**, the LLM rewrite being only one: word
  swap, negation, intensity change ("always"/"sometimes"/"never"),
  condition change, gene deletion/duplication, borrow-from-founder-pool.
- **Hard length cap per gene** (e.g. ≤ 25 words) and a fixed number of loci.
- **Several rewrite styles** chosen at random (invert, specialise,
  generalise, exaggerate, add a condition) instead of a single generic
  "random change" prompt — still "without context" about fitness.
- **Cache + log every mutation** (parent gene, operator, child gene, seed)
  so runs are reproducible and students can study mutation effects.

### R4 — Crossover of unstructured prompts is meaningless
"Take half the genes from each parent" only works if **gene *i* in parent
A and gene *i* in parent B are about the same thing** (homologous loci).
Otherwise a child may inherit two hunger genes and no fear gene.

**Mitigation:** fixed **loci with semantic roles** (hunger, fear,
exploration, social, terrain, mating, aggression…). Uniform crossover per
locus. Optionally allow a few "free" loci for open-ended evolution.

### R5 — Order/position bias
LLMs weight early and late text differently. Concatenation order becomes a
hidden gene. **Mitigation:** fixed locus order (same for everyone), or
present genes as a numbered list; mention this as a student experiment.

### R6 — Implicit selection is noisy; populations can crash
With few agents and stochastic food, early luck dominates, and a whole
population can die before anything evolves. **Mitigation:** minimum
population floor that respawns from the **founder pool** (or a hall of
fame), not from random text; tunable food growth; log per-lineage stats.

### R7 — Hardware heterogeneity among students
Some will have no GPU. **Mitigation:** every backend behind one interface,
including a **rule-based mock backend** that parses a few keywords from
genes — instant, deterministic, lets students work on everything except the
model itself. Plus a shared lab server option.

---

## The design decision I would push hardest on

**Decide between (A) "LLM as brain" and (B) "LLM as development".**

- **(A) LLM as brain** (the idea as stated): genome text is in the prompt at
  every decision. Maximal expressiveness, highest cost, the risk profile
  above.
- **(B) LLM as development (genotype → phenotype at birth):** once per
  agent, at birth, the model reads the genome and outputs a **typed
  phenotype** — e.g. utility weights for each action, thresholds, preferred
  distances, a small behaviour tree as JSON. The agent then runs that
  phenotype cheaply every tick.
  - Cost: **one LLM call per birth** instead of per decision → 100–1 000×
    cheaper, runs fine on CPU, scales to hundreds of agents.
  - Biologically apt (DNA → development → organism) and pedagogically clean:
    genotype, phenotype and behaviour are three inspectable layers.
  - Still uses typed/structured output — just a richer schema.
  - Loses some context-sensitivity (the LLM can't "reason" in a novel
    situation) — but the phenotype schema can be as rich as needed.

**Recommendation:** build the framework so **both** are backends of the same
interface, ship **(B) as the default lab** (it will actually run in a
classroom), and offer **(A)** as the advanced/GPU track or a project topic.
Students comparing A vs B is itself a great assignment.

## Verdict

Worth doing. It is the right kind of modern and the right kind of simple —
*if* the gene → behaviour link is validated first, the genome has
homologous loci, mutation has guard-rails, and the runtime doesn't call an
LLM per agent per frame.
