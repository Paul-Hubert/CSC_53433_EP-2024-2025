Run **one very long simulation** of the prompt-genome prototype and collect statistics on **how the genes develop**: which genes spread or vanish, what mutation produces and what selection keeps, how behaviour follows, and whether genes stay meaningful. The run takes about 12 hours, so this prompt may be pasted more than once: always start with **Where are we**.

## Settings

Everything below uses these values. Edit them before pasting if you want another run.

| Setting | Value | Why |
|---|---|---|
| Brain | `--backend llm` (gemma4:12b, points mode) | the meaning of a gene decides, not its keywords; the keyword runs already reached 230 generations |
| World | `--profile small`: the flat Lab 1 world, 48 × 48, 2 predators, 10 to 40 animals | same as the hour-long run |
| Seed | `--seed 1234` | continues the hour-long run (`Docs/prompt-genome/10` §7): its 5 269 ticks replay from the cache, then the run goes on. Checked 2026-10-05: 1.8 s, no model call, events hash `6902f4f774db5327` |
| Length | `--minutes 720 --ticks 200000` | 12 hours; the tick count is only a ceiling |
| Name | `llm_long` | `results/runs/llm_long/`, `logs/llm_long.log`, `logs/run_llm_long.progress.json`, stop file `logs/run_llm_long.stop` |
| Checkpoints | every 500 ticks | about two generations |

Expected pace: about 1.9 brain calls per second (2 workers) and 2.2 calls per tick at 22 animals, so about 3 000 ticks per hour, fewer as the population nears 40. Twelve hours should reach tick 25 000–40 000, about 100–170 generations.

Keyword-brain alternative (cheap, but it reads keywords, not meaning): `--backend rule_based --profile full --seed 1234 --ticks 1000000 --minutes 600`, name `rb_long`, checkpoints every 5 000 ticks. That is about 4 600 generations in an estimated 3–5 hours; the 50 000-tick runs took 13 min each, three in parallel.

## Context

- Repository root: this directory. Code in `prototype/`: run everything from there with `.venv/Scripts/python`. Branch `claude/ai-agents-course-redesign-71pc1u`.
- Read first:
  - `prototype/STATUS.md`;
  - `prototype/CLAUDE.md` (working rules);
  - `Docs/prompt-genome/10-natural-selection-runs.md`: §2 how genes are ranked, §3 the keyword runs, §7 the hour-long run;
  - `Docs/prompt-genome/04-genome-and-evolution.md` §5 (mutation);
  - `prototype/results/llm_60min_genes.md`.
- The system:
  - Each animal has 10 genes, English sentences in fixed slots: 7 actions (eat, flee, follow, wander, rest, mate, attack) and 3 temperaments (risk, social, place).
  - Every 4 ticks the brain turns genes and situation into action probabilities.
  - A child takes each gene from one parent or the other. Each gene then mutates with p = 0.03: gemma4:12b gets one of 16 "random change" instructions (`prompts/mutate_v2.txt`) and the sentence, nothing else, at temperature 1.2.
  - There is no fitness function: animals that eat, escape predators and mate leave more children. Newcomers with founder genes keep the population at 10 or more; the cap is 40.
- What a run records (`promptevo/sim.py`, `promptevo/eventlog.py`). The whole gene history can be rebuilt from these files after the run:
  - `events.jsonl`:
    - every founder, newcomer and birth: genome, parents, generation, and mutations (parent gene, new gene, instruction index, text);
    - every death: cause, age, offspring, food eaten.
  - `stats.csv`, every 100 ticks.
  - At the end only: `alleles.jsonl` (every text, with its parent and instruction), `summary.json` and `final_population.json`.
- Tools:
  - `experiments/smoke_run.py`: the run;
  - `experiments/gene_report.py`: gene fitness relative to contemporaries, marks ★ ▲ ✗, frequencies, lineages;
  - `experiments/status.py`: background jobs;
  - `experiments/peek.py`: looks into JSONL and CSV files.
- Replay: every model answer is cached (`cache/ollama.sqlite`, `cache/policy.sqlite`) and a run is deterministic for its seed. Re-running a seed replays everything already computed at about 3 000 ticks per second.
- Environment:
  - Windows 11, Git Bash, Python in `prototype/.venv`;
  - Ollama 0.35.0 at localhost:11434, with gemma4:12b (digest `6114515d63c1`) as both brain and mutator. The hour-long run used Ollama 0.32;
  - RTX 5080 16 GB, about 5 GB of it used by other programs; 64 GB RAM.

## Rules

- Follow `prototype/CLAUDE.md`:
  - short script output;
  - jobs over 2 minutes in the background with a progress file, and no polling loops;
  - commit each step once its tests pass, and never push;
  - never delete `prototype/cache/`, never write keys.
- One run only. While it runs it owns the GPU, so start nothing else that calls a model; offline tests are fine.
  - Before launching, check `nvidia-smi`. If another program uses the GPU heavily (for example an ML-Agents training), stop and ask me.
  - Never stop or change other processes.
- Measure the current system. Don't change the brain, the mutation, `prompts/`, `configs/` or anything else the run reads. The replay check below must keep giving events hash `6902f4f774db5327`, and a stopped run must stay resumable.
- Model budget outside the run: at most 300 new calls before the launch, and at most 3 000 for the optional judge in Phase 4. Cached answers are free.
- Git Bash corrupts backslashes in Python passed through heredocs. Write scripts to files in a scratch directory outside the repo, and run `python -m compileall -q experiments promptevo tests` after edits.
- Run directories are gitignored; commit the reports in `results/`.

## Where are we (start here each time)

1. Read `git log --oneline -8` and run `python -m experiments.status`.
2. Then pick the phase:
   - `smoke_run.py` has no stop file yet → Phase 1.
   - Phase 1 is done but there is no `results/runs/llm_long/` → Phase 2.
   - `run_llm_long` is `running` → leave it alone. Tell me the current tick, the ticks per hour since the replay and the expected end, then continue Phase 3.
   - `DEAD?`, `failed`, or `stopped` before the time limit → read the end of `logs/llm_long.log` and tell me why. Once I agree, resume by replay: the Phase 2 command with the minutes left.
   - `done` → finish Phase 3 if needed, then Phase 4.

## Phase 1 — Make a 12-hour run safe (before launching)

A failure at 3 am must neither lose the night nor poison the cache. Commit each item with its test, and keep the full suite passing.

1. **Clean stop.**
   - `smoke_run` checks for `logs/run_<name>.stop` every 500 ticks and stops the way `--minutes` does.
   - Ctrl-C or any exception also stops cleanly: `alleles.jsonl`, `summary.json` and `final_population.json` are written, and the progress file says `stopped` or `failed` with the reason.
   - With `--minutes`, the progress file shows the planned end time.
2. **No silent failures.**
   - A brain call that fails three times returns a uniform answer, the run carries on, and that answer is stored in `cache/policy.sqlite` permanently (`backends/ollama_policy.py`: `_one` returns it, `decide` stores it).
   - A failing mutator call raises inside `_birth` and kills the run before those files are written.
   - Fix: every model failure stops the run cleanly with a message, and a fallback answer is never cached.
3. **Survive a hard stop.**
   - Flush `events.jsonl` and `stats.csv` at every progress update, and rewrite `alleles.jsonl` every 5 000 ticks, so a power cut loses at most a few hundred ticks.
   - `gene_report`, and the Phase 3 script, must also read an unfinished run: rebuild the final population from the events and take the last tick from the last event.
4. **Resume by replay.**
   - A stopped run resumes by re-running the same command: the cached part replays, then the run continues.
   - Test it with a fake model that counts calls. Stop a run with the stop file, re-run it, and check two things: the events match an uninterrupted run, and the replayed part made no new calls.
5. **Behaviour over time.** Add cumulative decision counts per action (`act_eat` … `act_attack`) to `stats.csv`. The events must not change.
6. **Keep the PC awake.** On Windows, `smoke_run` asks the system not to sleep while it runs: `SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED)` through ctypes, and nothing on other systems. This changes no power setting and ends with the process.
7. **Record what ran.** At the start, write `results/runs/<name>/run_info.json` with:
   - the command line and git commit;
   - the Ollama version and model digests;
   - the `evolution`, `policy`, `agents`, `predators` and `world` config;
   - the start time.

Then check in a scratch directory: seed 1234, small profile, LLM brain, 5 269 ticks must make no new model call and give events hash `6902f4f774db5327`. If not, stop and tell me.

## Phase 2 — Launch

1. **Pre-flight:**
   - Ollama answers (`curl -s localhost:11434/api/version`);
   - gemma4:12b is listed with the digest above;
   - the GPU is free (`nvidia-smi`);
   - more than 5 GB of disk is free;
   - `git status` is clean and the tests pass.
2. **Launch** from `prototype/`, detached, so it doesn't depend on this conversation:

   ```bash
   nohup .venv/Scripts/python -m experiments.smoke_run --backend llm --profile small --seed 1234 \
         --minutes 720 --ticks 200000 --snapshots 40 --out results/runs/llm_long > logs/llm_long.log 2>&1 &
   ```

3. **Check once** with `python -m experiments.status` that it runs. The replay should take it past tick 5 000 within seconds.
4. **Record it** in STATUS.md › Background jobs, then commit:
   - the PID, start time and planned end;
   - how to check it;
   - how to stop it: `touch logs/run_llm_long.stop`, never killing the process;
   - how to resume it.
5. **Tell me** the planned end, and that a Windows Update restart, putting the PC to sleep by hand, or an Ollama update will stop the run. It then resumes by replay.
6. Continue with Phase 3 while it runs.

## Phase 3 — Build the gene-development analysis (while the run goes)

Write `experiments/gene_timeline.py`, with tests in `tests/test_gene_timeline.py`. Reuse `gene_report` instead of copying it: `Run`, `words`, the world-word list, `lineage` and the fitness method. Develop it on the finished runs `llm_60min` and `long_1234`.

`python -m experiments.gene_timeline RUN [--every 500] [--tag T] [--judge]` writes `results/<tag>_timeline.md`, `.json`, `.html` and `.csv` (one CSV row per checkpoint and slot). It answers six questions.

1. **How does the gene pool change?** Per checkpoint and slot:
   - population, and mean and highest generation;
   - distinct genes alive, and their effective number (1 / Σ share²);
   - the most common gene: its text and share;
   - share of founder texts versus mutants;
   - mean mutation depth: mutations since a founder text;
   - mean words per gene;
   - share of genes still using a word of the animal's world.
2. **Which genes rose and fell?** For every gene that reached 25 % of its slot at any checkpoint:
   - origin: founder, newcomer, or mutant (with its parent and instruction);
   - first appearance, and the ticks it took to reach 25 %, 50 % and 90 %;
   - peak share; whether it fixed, is still present or was lost, and when;
   - its lineage from the founder text, with the instruction at each step.

   Add a "leader per slot" table: the most common gene of each slot over time, with a row only when the leader changes.
3. **What does mutation offer, and what does selection keep?** Compare three sets of mutants: all those produced, those that had at least 5 living carriers at once, and those alive at the end. For each set give:
   - the count;
   - the instruction mix: small-edit versus big-change instructions, per the per-instruction table of `results/mutation_test.md`;
   - words changed from the parent, and length;
   - share using a world word.

   Put the mutation test, which has no selection, next to them.
4. **Selection or drift?**
   - Fitness relative to contemporaries, per gene and per reading, as in `gene_report`.
   - The ancestry check of 10 §7, for every gene that swept: compare the share of living animals that carry it with the share of their pedigree that comes from ancestors that carried it. A much higher carrier share points to selection on that gene. Equal shares mean it rode along with a successful family.
   - Families over time: how many founder and newcomer families still have living descendants, and when the whole population first descends from a single ancestor.
5. **How does behaviour follow?** Per window of 2 000 ticks:
   - action shares, from the new `stats.csv` columns;
   - births, deaths by cause, and predator kills per 1 000 animal-ticks;
   - mean lifespan, mean energy, newcomers.

   Line them up with the gene timeline, so a change in behaviour can be matched with the gene that spread at the same time.
6. **Do genes stay meaningful?**
   - Over time: world-word share, length, and how alike the living texts of a slot are (mean word overlap).
   - Optional `--judge`, run only after the run ends, when the GPU is free. A fixed prompt in `prompts/judge_sense_v1.txt` asks gemma4:12b (temperature 0, cached) whether a gene still gives an animal a usable rule for its slot: yes or no.
     - Judge only the texts that had at least 3 carriers at once, with at most 3 000 calls.
     - Report the result as one model's opinion, and check 30 of its answers by hand.

The HTML page is self-contained: inline SVG, no external libraries. matplotlib isn't installed; add no dependency without asking. The page shows:

- one Muller chart per slot: the stacked shares of the genes over time, each founder text's family in one hue, with labels on the genes that reached 25 %;
- lines for population, mean generation, mutant share, world-word share and the effective number of genes;
- behaviour shares over time.

Tests:

- the shares in each slot sum to 1 at every checkpoint;
- counts match the population;
- every lineage ends at a founder text;
- an unfinished run is read correctly;
- a tiny synthetic run gives known numbers.

When the script works, run it once on the live run and tell me what is visible so far.

## Phase 4 — After the run

1. **Check how it ended:** a `time limit` line in the log, `failures=0`, and the summary written. If it stopped early, see "Where are we".
2. **Run the reports** from `prototype/`:

   ```bash
   python -m experiments.gene_report results/runs/llm_long --min-carriers 25 --every 2000 --tag llm_long
   python -m experiments.gene_timeline results/runs/llm_long --every 500 --tag llm_long --judge
   ```

3. **Compare:**
   - with the keyword run `long_1234` at the same number of generations;
   - with the hour-long run: does "Never fight." stay, and do mutants take over?
4. **Be strict about evidence.** This is one seed with 10 to 40 animals, so drift is strong, and a gene that sweeps once is a lead, not a result. Say which conclusions the numbers support and which they don't.

## Deliverables

1. **Committed results:** `results/llm_long_genes.md/.json` and `results/llm_long_timeline.md/.json/.csv/.html`.
2. **A new page `Docs/prompt-genome/11-gene-development.md`**, written like page 10, covering:
   - the run;
   - the gene pool over time (key rows only);
   - the genes that swept, with their lineages;
   - mutation versus selection (the three-set table);
   - selection or drift;
   - behaviour;
   - meaning;
   - what it means;
   - how to reproduce it.

   Then:
   - link it from the README index and from 10 §7;
   - add short entries to 06 (results) and 09 (history);
   - add the key numbers to `prototype/STATUS.md`, keeping it at 150 lines or fewer.
3. **Show me:**
   - a summary of 10 lines or fewer;
   - the leader-per-slot table;
   - the mutation-versus-selection table;
   - the HTML page.

   Then stop.
