# 32 — Integrity prompts and CI

Two kinds of integrity, both checked in CI and usable by hand:

1. **The models do what the contract assumes**: genes steer the brain the way
   they say, random text doesn't, prompts fit, mutations stay meaningful. These
   checks send prompts to the real brain and mutator (§2).
2. **The implementation keeps the contract**: a coding agent audits the Unity
   code against these documents with the prompts of §3.

The owner's guidance: CI may call real LLMs, and its duration is not a concern.

## 1. Where each check runs

| Tier | Content | Runner | Trigger | Blocks |
|---|---|---|---|---|
| T0–T1 | validation, static checks, EditMode tests | GitHub-hosted (game-ci) or self-hosted Unity runner | every push | merge |
| T2 | PlayMode tests, short scenarios with fakes | same | every push | merge |
| T3 | regression: hashes, ranges, soak, performance | self-hosted Unity runner | nightly, before a release | release |
| T4 | brain and mutator integrity (§2), LLM scenarios S06, S10, S13, S15, S22, S26, S27 | self-hosted GPU runner with Docker (vLLM for JEV) and Ollama | nightly, manual | B-01 blocks; gates are tracked as trends |
| T5 | coding-agent audit (§3, prompt P1) | Claude Code (GitHub Action or a scheduled cloud session) | weekly, before a release | a report a person reviews |

## 2. Brain and mutator integrity checks

Each check writes a JSON and a Markdown report with the model names, digests or
pinned revisions, prompt ids and the date, so that results can be compared over
time.

| Id | Check | Pass |
|---|---|---|
| **B-01** | **Smoke**: one query per species with founder genes and three situations; valid rows; latency recorded | every row valid (blocking) |
| **B-02** | **G1 directed**: every contrast pair (pro / anti, neutral elsewhere) × the relevant observations of the 48-situation set | sign accuracy ≥ 85 % and mean ΔP ≥ 0.25 |
| **B-03** | **G2 information**: 10 founder genomes, 10 random-text genomes, the neutral genome × 24 situations | MI_G(founders) ≥ 2 × MI_G(random text); full thresholds reported |
| **B-04** | **G3 locality** (needs the mutator): 10 parents × 4 single mutations | locality ratio ≤ 0.5, Spearman ρ ≥ 0.3 |
| **B-05** | **Option order** (multiple-choice brains): the same queries with options permuted | report the share of changed top answers (JEV: 11.5 % on 16 options); requests always use the species' order |
| **B-06** | **Prompt length**: the longest founder genome of every species with its longest situation | under the brain's limit; the run's too-long counter stays 0 |
| **B-07** | **Points sanity** (Ollama): share of answers summing to 100, share of all-zero answers, by genome kind | reported; all-zero answers on founder genes fail |
| **B-08** | **Names**: S26 (a renamed species) | P(flee) drops for "Run from any wolf you see." when wolves are renamed |
| **B-09** | **Repeatability**: the same 50 queries twice with the cache off | identical answers (Ollama: temperature 0 and a seed); JEV: reported differences above 1e-3 |
| **B-10** | **Mutator**: the 9 × 4 founder sentences through the deck, then 10 mutations in a row without selection, judged by the judge prompt below | guards pass ≥ 70 % of first answers; ≥ 80 % usable after one mutation; usable share after 10 reported (prototype: 50 %) |
| **B-11** | **Blindness**: the mutator's received prompts | contain only the context line, one instruction, the sentence and the reply line (MUT-11) |

**Gate policy.** A gate that a model is known to fail (G2 with gemma and JEV,
2026-10-09) is not blocking; it fails CI only when its value drops below the last
accepted value by more than its bootstrap margin. A new model or prompt is
accepted by a person, who records the new values.

**The judge prompt** (from the prototype, used by B-10 and S13):

```text
Animals in a simulation act on short English rules called genes. Each gene sits in a slot. The slot "{slot}" is about {topic}.

The gene in the slot "{slot}" is: "{text}"

Does this gene still give the animal a usable rule about {topic}? A usable rule is understandable and could guide what the animal does, even if it is odd or unwise. Nonsense, or a sentence about something else, is not a usable rule.

Answer yes or no.
```

**Brain prompts.** The checks use each species' real prompt ([08 §7](08-decisions-brains-and-prompts.md#7-the-prompt)),
assembled or frozen, exactly as the world sends it; the reports store the prompt
ids.

## 3. Prompts for coding agents

Written in the style of the owner's prompts in `Docs/prompts/`: context, rules,
what to check, what to deliver. Paste them into a Claude Code session at the root
of the Unity project (T5 runs P1 on a schedule).

### P1 — Audit the implementation against the contract

```text
You are auditing the Unity implementation of the evolving-animals system against its
contract. Work in two phases. Phase 1: find and prove problems, change nothing.
Phase 2: fix them, only after I approve the list.

Context
- Contract: Docs/Interface/ (README first; rules are numbered, e.g. SENSE-03, and each
  MUST rule names the tests that cover it in 30-tests.md).
- Implementation: Assets/EvoSim/ (Runtime, Editor, Tests). Architecture: 20-unity-architecture.md.
- Run tests with the Unity Test Framework in batch mode (EditMode and PlayMode).

Rules
- Phase 1 is read-only for the repository; throwaway code goes in a scratch folder.
- Prove every finding: a failing test, a small script and its output, or a file:line with a
  concrete input that goes wrong. If you can't make it fail, call it a risk, not a bug.
- No real LLM calls unless I say so; use ScriptedBrain and FakeMutator.
- Summaries of at most 20 lines per command; details to files.

What to check
1. Every MUST rule: is there a test citing it, does the test really check the rule, does it pass?
2. Discovery and ownership (ARCH-04/05/06): nested species, disabled modules, gene binding,
   order and signature.
3. Determinism (RAND-01..13): global randomness, unordered iteration, wait modes, ticks per
   frame, rendering; run the same seed twice and compare hashes.
4. Animals as data (CORE-05, ARCH-03): no per-animal MonoBehaviour logic, no per-animal state
   hidden in modules, no statics (ARCH-07).
5. The brain boundary (CORE-06, ACT-20) and blind mutation (CORE-07, MUT-11).
6. Tick order (TICK-04) and the act order policies (ACT-30/31).
7. Outputs (OUT-01..05): rebuild genomes from the events; no secrets anywhere (OUT-04).
8. Validation (EDIT-01): every validator has a triggering fixture.

Deliver
A table: rule id | finding | evidence | severity (bug / risk / doc) | proposed fix.
Then the list of MUST rules with no real test. Stop and wait for my approval.
```

### P2 — Review a student's extension

```text
Review this change, which adds a module to the evolving-animals system (a sense, an action,
a gene kind, a stat, a brain, a mutation operator or a phase).

Check, with evidence:
- It derives from the right base class and overrides only what the recipe in
  Docs/Interface/22-extending-recipes.md says.
- The conformance suite of its kind (Docs/Interface/30-tests.md §4) passes, and the student
  added the tests the recipe asks for.
- No per-animal state inside the module (CORE-05); no global randomness (RAND-01); actions
  only produce intents (ACT-06); a sense returns declared tokens (SENSE-01) and the
  observation space stays reasonable (SENSE-05).
- Validation is green; if the species' signature changed, the change is intended.
- The prompt preview reads well and stays under the brain's limit (PROMPT-06).

Deliver: what works, what breaks a rule (rule id, file:line), what to improve, in that order.
Be kind and concrete: this is a student's work.
```

### P3 — Investigate a regression

```text
A regression test failed: <R-01 pinned hash for <scene>/<seed> | R-02 range <metric>>.
Find why, without changing the pinned values.

1. Reproduce it locally; record the old and new values.
2. Find the first tick and the first event where the two runs differ (dump both events
   files and compare); name the phase and the module that wrote it.
3. Find the commit that introduced it (bisect if needed).
4. Classify: an intended behaviour change (then say which rules and documents must change
   and propose the re-pin with its justification), or a bug (then propose a fix and a test
   that would have caught it).
Deliver a short report with the evidence.
```

### P4 — Keep the contract in step with the prototype

```text
The Python prototype (prototype/) changed since the commit the contract was written
against (see Docs/Interface/README.md, "Status"). List every behaviour change between
that commit and HEAD that touches a rule of Docs/Interface/ or a reference value of
14-configuration-reference.md. For each: the commit, the old and new behaviour, the
affected rule ids, and the proposed new wording. Ignore refactors that don't change
behaviour. Change nothing until I approve.
```

### P5 — Write the tests for a rule or a module

```text
Write tests for <rule id(s) | module class>, following Docs/Interface/30-tests.md: the
naming (T-<AREA>-NN), the helpers of EvoSim.Testing (WorldBuilder, ScriptedBrain,
FakeMutator, Place, Stat, Golden), fixed seeds, statistical asserts within 4 standard
errors, and a citation of the rules in each test's description. Add each new test to the
list in 30-tests.md. Run the tests and show that each fails when the behaviour is broken
on purpose (then restore it).
```

### P6 — Investigate a brain gate that moved

```text
Brain integrity check <B-02 | B-03 | B-04> changed from <old> to <new> for <model, prompt id>.
Break the result down by locus and by observation tag; list the 10 queries whose answers
changed most, with their prompts and both answers; say whether the prompt, the model
revision, the situation texts or the founder pools changed (compare prompt ids and
digests in the two reports). Propose the smallest change that would restore the gate, and
the cost of measuring it (model calls).
```

## 4. The CI pipeline

```yaml
# .github/workflows/evosim.yml (sketch)
on:
  push:
  schedule: [{ cron: "17 2 * * *" }, { cron: "17 3 * * 1" }]   # nightly T3 + T4; weekly T5
  workflow_dispatch:
jobs:
  editmode:                                   # T0 + T1
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/cache@v4
        with: { path: Library, key: library-${{ hashFiles('Packages/packages-lock.json') }} }
      - uses: game-ci/unity-test-runner@v4
        env: { UNITY_LICENSE: "${{ secrets.UNITY_LICENSE }}", UNITY_EMAIL: "${{ secrets.UNITY_EMAIL }}",
               UNITY_PASSWORD: "${{ secrets.UNITY_PASSWORD }}" }
        with: { testMode: editmode, unityVersion: 6000.3.x }
  playmode:                                   # T2
    needs: editmode
    runs-on: ubuntu-latest
    steps: [ ...same with testMode: playmode... ]
  regression:                                 # T3
    if: github.event_name != 'push'
    runs-on: [self-hosted, unity]
    steps: [ checkout, "unity -batchmode -executeMethod EvoSim.Batch.RunRegression -quit" ]
  llm-integrity:                              # T4
    if: github.event_name != 'push'
    runs-on: [self-hosted, gpu]
    steps:
      - checkout
      - "docker compose -f prototype/docker/compose.yaml up -d && wait for /health"   # JEV on vLLM
      - "ollama pull qwen3.5:0.8b && ollama pull gemma4:12b"
      - restore answer caches (actions/cache, keyed by model revisions and prompt ids)
      - "unity -batchmode -executeMethod EvoSim.Batch.RunIntegrity -quit"            # B-01..B-11
      - "unity -batchmode -executeMethod EvoSim.Batch.RunScenarios -tier T4 -quit"
      - upload reports as artifacts; compare gates with the accepted values
  agent-audit:                                # T5, weekly
    if: github.event.schedule == '17 3 * * 1'
    runs-on: ubuntu-latest
    steps:
      - uses: anthropics/claude-code-action     # with prompt P1, read-only phase 1
        with: { anthropic_api_key: "${{ secrets.ANTHROPIC_API_KEY }}" }
```

Notes:

- **Secrets** (`UNITY_LICENSE`, `ANTHROPIC_API_KEY`, `OLLAMA_API_KEY` for a cloud
  model) live only in the CI's secret store and reach the code as environment
  variables (OUT-04). T-OUT-03 checks that none leaks into outputs.
- **Answer caches** are kept between nightly runs, keyed by model revision and
  prompt id, so a nightly T4 run only pays for what changed. A weekly run
  clears them to measure the models afresh.
- **Pinned models**: JEV by repository revision, Ollama models by digest; the
  reports record both. A digest change is reported as such, so a moved gate can
  be traced to the model rather than the code.
- **Duration**: T4 with JEV (S27 at 500 ticks ≈ 25 min, gates ≈ 1 h) and the
  mutator checks fit a nightly window; long evolution runs (G4) stay manual.
