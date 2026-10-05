# 11 — Extending the prototype

Practical recipes for changing the Python prototype, and the places where it hard-codes things (the coupling points that motivate the Unity architecture).

General rules (prototype `CLAUDE.md`): every randomness through a named stream
(`promptevo.rng`), every model call through the sqlite cache, every script takes
`--profile` and prints ≤ 20 lines, run `pytest -q` before committing. The founder
pool is meant to be frozen and versioned: change texts in a **new** version
(`founder_pool_v2.json`), not in v1.

## Recipe: add an action (example: `drink`)

An action is also a locus, so this touches the genome, executors, brains and data.

1. **Locus and action order** — add `"drink"` to `ACTION_LOCI` in
   `promptevo/genome.py`. Position matters: it sets the column in every
   probability vector and the gene order in the genome key, so all genome keys
   change (old cache entries are simply no longer hit).
2. **Executor** — write `do_drink(agent, world, agents, cfg, rng) -> bool` in
   `promptevo/actions.py` (return whether the agent moved; call `_invalid()` when
   there is no target) and add it to `EXECUTORS`. Add any world query it needs
   to `World` (e.g. nearest water-adjacent cell). If drinking changes a new
   state variable, add the field to `Agent` (`sim.py`) and its cost to
   `Simulation.step()`.
3. **Allele data** — create `data/founder_pool_v2.json` with a `drink` entry
   (`neutral` + 4 alleles) and `contrast_alleles_v2.json` with a pro/anti pair;
   `AllelePools` raises `KeyError` if any locus is missing. Load v2 by passing
   `pools=AllelePools(reg, data_dir, version="v2")` to `Simulation` (or change
   the default).
4. **Rule-based brain** — add `"drink"` to `ACTION_WORDS` (required:
   `gene_weight` looks it up), a default logit in `default_logits`, and, if
   relevant, temperament deltas and condition phrases.
5. **LLM prompt** — copy `prompts/teacher_v1.md` to `teacher_v2.md`, add the line
   `- drink: …` to the action list, and point `policy.prompt` at it. Schemas and
   the logprobs word list are built from `ACTIONS` automatically; the prompt hash
   changes the cache key automatically.
6. **Directed tests** — add `"drink"` to `RELEVANT_TAG` in `perception.py`, with a
   tag in `Observation.tags()` if no existing tag fits.
7. **Laya (if used)** — add a neutral description to `NEUTRAL_OPTIONS` in
   `backends/laya_backend.py`.
8. **Renderer** — `render.py` draws the first letter of the action; pick a
   distinct letter if needed.
9. **Tests** — an executor scenario test (like `tests/test_behaviour.py`),
   `test_rule_based_directed_and_gibberish` covers the new locus automatically
   once the contrast pair exists; `test_pools_loaded` expects 5 options per
   locus.

## Recipe: add an observation field (example: `thirst`)

1. **Dataclass** — add the field to `Observation` in `perception.py` **with a
   default value** at the end: tests and `make_obs.load_obs` construct
   observations positionally or from old JSONL rows.
2. **Perception** — compute and discretise it in `sense()`. Keep the value set
   small: each extra value multiplies the number of distinct situations and
   lowers the memo and cache hit rates (and raises LLM cost).
3. **Text** — extend both styles in `obs_text.render`: V1 (`… Thirst: high.`) and
   V2 (a sentence per value; V2 uses dict lookups, so a missing value raises
   `KeyError`).
4. **Rule-based brain** — add `CONDITIONS` phrases (e.g. `thirsty → thirst ==
   "high"`) and adjust `default_logits` if an action should react by default.
5. **Tags** — add a tag if a directed test depends on it.
6. **Observation set** — extend `make_obs.synthetic()` and regenerate as
   `data/observations_v2.jsonl` (keep v1 for comparison).
7. **Prompts** — usually nothing: the situation text is inserted as is.

## Recipe: add a decision backend

1. Create `promptevo/backends/my_backend.py`:

   ```python
   class MyBackend:
       name = "my_backend"
       def decide(self, queries: list[Query]) -> np.ndarray:
           out = np.zeros((len(queries), len(ACTIONS)))
           for i, q in enumerate(queries):      # q.genes: {locus: text}, q.obs: Observation
               out[i] = ...                      # scores in ACTIONS order
           return normalise(out)                 # rows sum to 1, no zeros
   ```

2. If it calls a model, cache per query with `KVCache` +
   `make_key("my_backend", model_id, version, q.genome_key, render(q.obs, style))`
   and record the model id/digest with results.
3. Register it in `backends/factory.make_backend` (one `if` branch) and in the
   error message.
4. Test with `tests/` fakes; then run `python -m experiments.e1_sensitivity
   --backend my_backend --tag mine` — the E1 suite and gates work for any backend.
5. For option B ("development at birth"), the protocol still fits: compute a
   phenotype once per `genome_key` and evaluate it per observation inside
   `decide`.

## Recipe: add a mutation operator

1. Write `op_x(text: str, rng) -> str | None` in `evolution/mutation.py` (return
   `None` when it does not apply). Use only the `rng` passed in.
2. Add it to `WORD_OPS`.
3. Add a weight under `evolution.operators` in `configs/base.yaml`. The `Mutator`
   builds its menu from that dict; names not in `WORD_OPS` (other than
   `founder_reintroduce` and `llm_rewrite`) raise `KeyError`.
4. Output passes through `clean` and `valid` automatically; per-operator
   `{tried, ok}` appear in the run summary and the operator name in `birth`
   events and `alleles.jsonl`.
5. Test it like `tests/test_evolution.py::test_word_operators`. To measure its
   effect on behaviour, add it to the edit list in
   `e1_sensitivity.single_edit` (hard-coded) and compare locality.

## Recipe: add a metric or experiment

- **Metric** — a pure function in `promptevo/metrics.py` over arrays with the
  action axis last (`P[g, o, a]`); unit-test it on a synthetic case with a known
  answer (see `tests/test_metrics.py`). Wire it into
  `e1_sensitivity.evaluate()` and `report_lines()`.
- **Experiment** — a new `experiments/<name>.py`: `argparse` with `--profile`,
  `load_config`, generators from `default_rng(cfg.seed)` or `Streams`, a
  `Progress(resolve(cfg.paths.logs_dir), job, total)` for long jobs, results to
  `resolve(cfg.paths.results_dir)`, a ≤ 20-line printed summary. Reuse
  `build_sets` / `evaluate` instead of re-implementing the suite.
- **Run analysis** — read `events.jsonl` + `alleles.jsonl` of a run folder
  (allele frequencies per locus = count genome ids of living agents over time).
  `analyze.py` and `common_garden.py` are still to be written (☐).

## Recipe: swap the LLM model

1. Probe it: `python -m experiments.e0_probe_ollama --teacher <tag> [--mutator
   <small-tag>]` → `results/e0_ollama.md` (structured output, determinism,
   logprobs usable?, seconds per decision).
2. Set `policy.model` (decision) and `ollama.mutator_model` (mutation) in a
   config; choose `policy.mode` (`logprobs` only if the probe says
   `"logprobs_mode_usable": true`).
3. Cloud: either a cloud tag through the local server after `ollama signin`, or
   `ollama.host: https://ollama.com` with `export OLLAMA_API_KEY=…`.
4. Re-run the gate: `python -m experiments.teacher_gate --modes points,logprobs
   --workers 4` (or `--model <tag>`), then E1 with `--backend llm`.
5. Start simulations short (`smoke_run --backend llm --ticks 500`), read
   `llm_calls`, extrapolate before long runs. Caches are keyed by model and
   digest, so answers from different models never mix.

## Coupling points

Places where the prototype hard-codes structure. Each is a reason for an
explicit registry or interface in the Unity design.

| What is hard-coded | Where | Consequence when extending |
|---|---|---|
| `ACTIONS = ACTION_LOCI` tuple (7 names, fixed order) | `genome.py` | Action set = action loci = probability columns; one change ripples to every backend, schema, data file and metric |
| `TEMPERAMENT_LOCI` names `risk`, `social`, `place` | `genome.py`; **re-listed literally** in `rule_based.RuleBasedBackend.logits`, `ollama_policy._genes_block`, `laya_backend.build_request` | Adding or renaming a temperament locus silently drops it from the brains unless all three are edited |
| `LOCI` order and length | `Genome.__post_init__`, `genome_key`, `crossover_uniform` | Genome keys and caches are order-dependent |
| `EXECUTORS` dict | `actions.py`, called by name in `sim.step` | No registration mechanism |
| Action descriptions in the prompt | `prompts/teacher_v1.md` (free text) | Not generated from `ACTIONS`; easy to get out of sync |
| `Observation` dataclass fields and value sets | `perception.py`, `obs_text.render` (V1/V2), `rule_based.default_logits` / `CONDITIONS`, `Observation.tags`, `make_obs.synthetic` | A new sense touches 5 files; V2 lookups raise on unknown values |
| `RELEVANT_TAG` per action | `perception.py` | Needed by the directed tests for every action |
| Per-action keyword tables | `rule_based.ACTION_WORDS` (lookup fails if missing), `default_logits`, `laya_backend.NEUTRAL_OPTIONS` | One entry per action in each |
| Mutation lexicons (English) | `evolution/mutation.py` (`LADDER`, `NEGATIONS`, `CONDITIONS`, `SYNONYMS`, `LLM_STYLES`), fallback list `intensity/negate/condition_swap` | Not configurable; language-specific |
| Death causes, stats columns | `sim.py` (`"starvation"`, `"attacked"`, `"predator"`, `"old_age"`, `stats_row`) | New causes need log/CSV changes |
| Backend names | `backends/factory.py` `if` chain | No plug-in discovery |
| Allele file version `v1` | `founder.py` default; `Simulation` builds pools without a version | Switching versions needs code |
| Energy rules | `sim.step` (cost branches), executors (gains) | Metabolism is not a separate component |
| Reproduction rule | `sim._breed` / `_birth` (pairing, cost split, child placement) | No `IReproductionRule`; crossover choice not configurable |
| E1 sizes, gate thresholds, edit operators | `experiments/e1_sensitivity.py` (`SIZES`, `GATES`, `single_edit`), `teacher_gate.py` | Changing gates means editing code (and plan 08 §A2 forbids it after S6) |
| Display letters | `render.py` (first letter; predators `W`) | `flee`/`follow` collide; `wander` looks like a predator |

The Unity-side answer to these is proposed in [`../unity/README.md`](../unity/README.md).
