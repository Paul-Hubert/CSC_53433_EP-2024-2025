# promptevo — Phase 0 spike (LLM brain via Ollama)

Headless Python test of "genes = text prompts, an LLM decides, an LLM mutates".
- Plan: `../Docs/redesign/08-phase0-spike-plan.md`. Read ONLY B0, your session
  section, and the Part A sections that session lists under "Reads".
- State: `STATUS.md` is the single source of truth between sessions.

## Session protocol
- Start: read STATUS.md, run `git log --oneline -8` and `python -m experiments.status`
  (once it exists), then continue from STATUS.md › Next action.
- Work one step at a time. A step is done when its tests pass and it is committed:
  `git commit -m "spike S<n>.<step>: <what>"`.
- At every ⏸ in the plan: update STATUS.md, commit, then STOP and tell the user
  "⏸ Safe to compact — suggested: /compact <focus text from the plan>". Wait.
- Session end: set STATUS.md › Next action to the next session's first step, commit,
  then say "✅ Session <n> complete — /clear, then the kickoff prompt".
- If context grows large mid-step, write the exact position into STATUS.md first.
- Stop and ask at human checkpoints (H1–H5) and before changing gates in plan §A2.

## Output hygiene (context is the scarce resource)
- Scripts print a summary of ≤ 20 lines; details go to `results/` or `logs/`.
- `pytest -q -x`; pipe long output through `| tail -n 30`.
- Never cat JSONL/CSV/logs: use `python -m experiments.peek FILE -n 5`.
- Jobs > 2 min: `nohup python -m experiments.X ... > logs/X.log 2>&1 &`, writing
  `logs/X.progress.json`. Check with `python -m experiments.status`. No polling loops.
- Read third-party code (laya package, notebooks) via a subagent that returns ≤ 30 lines.
- Put discovered facts (API shapes, timings, decisions) in STATUS.md right away.

## Conventions
- Python ≥ 3.10. Package `promptevo/`, scripts `experiments/`, configs `configs/*.yaml`
  (profiles `small` / `full`). Every script takes `--profile`.
- All randomness via `promptevo.rng` named substreams; no global `random`/`np.random`.
- Every model call goes through the sqlite cache in `cache/`; never delete caches unasked.
- Record model digests / checkpoint ids with every result.
- Decision backend = Ollama LLM (`--backend llm`, rev. 2026-09-30); Laya is parked.
  Ollama runs locally (http://localhost:11434) or in the cloud (https://ollama.com with the
  key in the env var `OLLAMA_API_KEY`). Never write keys to files, configs or logs.
- LLM calls are the expensive resource: start runs short (`--ticks 500`), read llm_calls,
  extrapolate before launching long runs.
- Don't edit files outside `prototype/` (exception: `Docs/redesign/` in S7).
