You are auditing the prompt-genome evolution prototype for mistakes. Work in two phases. **Phase 1: find and prove problems, change nothing.** **Phase 2: fix them, only after I approve the list.**

## Context

- Repository root: this directory. Branch `claude/ai-agents-course-redesign-71pc1u` (see `git log -5` for the latest commits).
- The system: animals whose genes are 10 English sentences. An LLM (gemma4:12b via a local Ollama server) or a keyword brain turns genes plus situation into action probabilities, and evolution rewrites the sentences. It becomes Lab 1 of a course, running on a flat world with food at random.
- Code: `prototype/` (package `promptevo/`, scripts `experiments/`, configs `configs/`, data `data/`, prompts `prompts/`, tests `tests/`).
- Documentation of the system as built: `Docs/prompt-genome/` (01–09). It was fact-checked against the code on 2026-10-01.
- Live state: `prototype/STATUS.md`. Working rules: `prototype/CLAUDE.md`. Read both first.
- Environment: Windows 11, Git Bash, Python 3.13 in `prototype/.venv` (call `prototype/.venv/Scripts/python`), Ollama 0.32 with gemma4:12b and gemma4:26b, RTX 5080 16 GB.
- Tests: `cd prototype && .venv/Scripts/python -m pytest -q` (43 offline tests; `-m ollama` makes two real model calls).

## Rules

- Phase 1 is read-only for the repository. Write throwaway scripts in a scratch directory outside the repo.
- Never delete or edit `prototype/cache/` (LLM answer caches) or `prototype/results/`.
- LLM budget: at most 50 new model calls in total. Cached answers are free. Never start anything that runs longer than 2 minutes without asking. Rule-based simulations run in seconds, so use them freely.
- Prove every finding: a failing assertion, a small script with its output, or an exact `file:line` plus a concrete input that goes wrong. If you can't make it fail, call it a *risk*, not a *bug*.
- Follow the output hygiene in `prototype/CLAUDE.md`. Look at data files with `python -m experiments.peek`.

## Already known (don't report these as new)

- Gate G2 fails: random-text genes move behaviour as much as real genes. All-zero LLM answers become a uniform distribution, and that fix awaits an owner decision.
- Logprobs mode is unusable with gemma4. Throughput is ≈ 4 decisions/s.
- In one 500-tick run the LLM-read founders were less viable.
- Mutation is blind by design (owner decision 2026-10-02): without selection, genes leave the animal's world after 10–15 mutations (`results/mutation_test.md`). Don't report the drift itself as a bug.
- The founder pool is a draft. Analysis tools, the evolution matrix and the common garden are not written. Laya is parked.

## Recently fixed (verify the fix holds; report only if still broken)

`think: false` + fixed `num_ctx` (27a5da1), empty request cache (ed485c5), Windows liveness check in `experiments/status.py`, ASCII map symbols and legend (5ee5d38, 852af86), flat Lab 1 world with food regrowth 0.0007 (da30a1d). Mutation redesigned as one blind LLM operator (dda3a61): it replaced the word operators, and `smoke_run` now mutates with every brain (`--no-mutation` to skip).

## Leads I noticed (confirm or reject, then look beyond them)

1. `promptevo/backends/rule_based.py`: condition and temperament patterns have no word boundaries. `strong` matches "strongest" in the founder gene "Follow the strongest animal nearby.". Check every pattern against every founder, contrast and control sentence.
2. Text I/O without an explicit encoding. This machine's default is cp65001 (UTF-8), but a cp1252 Windows locale would differ, and `data/contrast_alleles_v1.json` contains non-ASCII.

## What to check

1. **Simulation** (`sim.py`):
   - tick order against `Docs/prompt-genome/03` §10;
   - energy accounting per action, breeding conditions and the cap, predation, deaths, the floor;
   - newcomers deciding on their first tick;
   - the memo key under the shuffled control;
   - counters and `summary()`;
   - determinism: same seed → same `events_sha`, also with `--world terrain_preview`.
2. **World** (`world.py`):
   - generation with zero and non-zero fractions, connectivity retry;
   - borders and movement (`step_toward` stuck logic, `step_heading`);
   - predators, food regrowth.
3. **Perception and text** (`perception.py`, `obs_text.py`): bucket thresholds, `tags()` and `RELEVANT_TAG`, the "432 observations" claim, V1/V2 rendering.
4. **Actions** (`actions.py`):
   - each executor;
   - the invalid → wander fallback and how it is counted;
   - attack once per decision period, the success formula;
   - mate and follow adjacency.
5. **Genome and evolution** (`genome.py`, `founder.py`, `mutation.py`):
   - registry dedup and `genome_key`, crossover;
   - the mutator: instruction drawing and seeds from the mutation stream, the prompt carries only the instruction and the gene, both guards (`clean`, `valid`), `stats`, events and allele metadata;
   - no mutation without a model; `smoke_run` and `e1_sensitivity` when Ollama is down (`MutatorUnavailable`).
6. **Brains**:
   - `rule_based.py` (regexes, conditions, "unless");
   - `ollama_policy.py`: prompt building, schema, normalisation, failure path, table / ksample / logprobs modes, the two cache keys, thread safety with `workers`;
   - `factory.py`.
7. **Ollama client and cache** (`llm/ollama_client.py`, `cache.py`): option and `think` merging, cache keys, retries and back-off, digest lookup, `KVCache` locking.
8. **Scripts**:
   - `smoke_run` (flags, `--world`, rewriter only with the LLM brain);
   - `teacher_gate` and `e1_sensitivity`: are MI, JSD, ΔP with relevance masks, locality and Spearman computed as `Docs/prompt-genome/06` §2 defines them?
   - `make_obs`, `status`, `peek`.
9. **Configs**: `base.yaml`, `small.yaml`, `full.yaml`, `worlds/terrain_preview.yaml`. Values against comments and against the docs.
10. **Tests**: does each test check what its name says? Can any pass vacuously? What is untested in the recent changes?
11. **Docs and READMEs**: for every finding, check whether `Docs/prompt-genome/`, `prototype/README.md` or `STATUS.md` state the wrong behaviour too.
12. **Portability**: Windows, macOS and Linux paths, encodings, and the shell instructions in the docs.

## Phase 1 deliverable

Write `prototype/notes/code-review.md` and show me its summary:

| id | severity (bug / risk / inconsistency / nit) | area | file:line | what is wrong | evidence | impact | suggested fix |
|---|---|---|---|---|---|---|---|

Then add:

- counts per severity;
- the list of areas you checked and found clean;
- anything you couldn't verify, and why.

Sort the findings by severity. Then stop and wait for my approval.

## Phase 2 (only after I approve)

Fix the approved findings one per commit, each with a regression test that fails before the fix. Run the full test suite after each fix. Update `Docs/prompt-genome/` and `prototype/STATUS.md` wherever they describe the changed behaviour. Use commit messages like `fix: <what>`. Don't push.
