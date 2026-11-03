Bring **all of EvoSim's documentation** in line with the code as it stands, above all the neural species work (M16, commits `0f3efd6`…`HEAD` on `llm-evolution`). The last full pass was `2fdd431`; it checked everything up to M15. This is a docs-only job: code changes only in `///` comments. Paste this prompt again to continue; always start with **Where are we**.

## Settings

| Setting | Value |
|---|---|
| Branch | `llm-evolution`. `git pull` before you start and before every push; never force-push, never rebase pushed commits. |
| Scope | `Docs/Interface/` (the contract), `Docs/Site/` (the students' website: `guides/`, `extending/`, `index.md`, `toc.yml`), `Docs/Neuro/PLAN.md` (its §12 readings only), `///` comments in `Assets/EvoSim/`. Not `Docs/redesign/` (history), not `Docs/prompt-genome/`, not `design.md`/`design.pdf`. |
| Unity | Don't start Unity and don't run tests: an M16.9 measured run may hold the project (check `Temp/UnityLockfile` and the status file). Docs-only changes need no test run. |
| Status file | `Docs/Interface/50-implementation-status.md` stays under 200 lines; move finished M16.1–M16.8 details to `51-implementation-log.md` (at its end, as M0–M15 were). |
| Site build | `powershell -File Docs\Site\build.ps1 -Strict` if DocFX is installed (`Docs/Site/README.md` § Install once); otherwise `Docs\Site\tools\audit.ps1` alone, and say so in the commit. |
| Publishing | Don't publish the site (gh-pages). At the end, tell the owner it is ready to publish. |

## Context (read every session)

1. `Docs/Interface/50-implementation-status.md`: **Next step**, the M16 rows, "Decisions taken while implementing", "Known problems".
2. `Docs/Neuro/PLAN.md` §1 (the owner's decisions), §4 (core changes C1–C8 and the rule text they need in 07, 08, 09, 13, 30, 31), §12 (readings taken while implementing).
3. `Docs/Interface/README.md` (the chapters and their order) and `Docs/Site/README.md` § Writing a page (the site's style rules).
4. `git log --stat 2fdd431..HEAD`: what changed. The code is the truth; where a doc and the code disagree, the doc changes. If the code looks wrong, don't fix it: list it for the owner.

## Rules

1. **Accurate first.** Every class, member, menu path, Inspector field, file name, rule id and number you write is checked in the code or a log. No invented values. When you can't check something, leave it as it is and list it for the owner.
2. **Say each thing once.** The contract (`Docs/Interface`) states the rules; the site explains and links to them. Don't copy rule text into the site.
3. **The contract's numbering stays stable.** Add rules at the end of their section with new ids; never renumber or reuse an id. A rule that no longer holds is marked *withdrawn (M16.x)*, not deleted.
4. **The site's style**: `Docs/Site/README.md` § Writing a page. Second-year students, plain English, short sentences, 80–220 lines a page, real code that compiles (```csharp blocks are compiled by the build).
5. **`///` comments** follow `Docs/Site/extending/documenting-your-code.md`. After touching `.cs` files, check `audit.ps1 --same-code HEAD`: nothing but comments changed.
6. **Commits**: one area per commit, titled `EvoSim docs: <what>`, with the attribution lines your session requires. Push at the end of each session (pull first).
7. **Output hygiene**: summaries of at most 20 lines; long output to `Logs/`; read it with `grep` and `tail`.

## Where are we (start here each time)

1. `git checkout llm-evolution && git pull`, then `git log --oneline -10` and `git status`.
2. Look for a line `Docs pass (09): <step> done` in the status file's **Next step**. Continue after the last step done; if there is none, start at step 1 below.
3. At the end of a session: update that line, commit, push, and say "✅ Session done — paste the prompt again to continue."

## Steps

### 1. Inventory (no edits)
Build a table in `Logs/docs-pass-09.md` (not committed): for each commit since `2fdd431`, what it added or changed (classes, rules, validation codes V-70…V-79, scenarios S29–S33, tests T-NEURO-01…16, files such as `vectors.jsonl` and `survival.json`, editor windows), and which doc pages should mention it. Then grep the docs for what is now stale: "text or number" gene kinds, "synchronous" neural brain, M16.x "next", old test counts, the old timeout of T-NEURO-16.

### 2. The contract (`Docs/Interface`)
- The rule text plan §4 asks for: 07 (self-directed actions, ACT-04), 08 (`DecisionQuery.Genome`, `Brain.ReadsGeneText`, attachments, the worker-task answer), 09 (vector alleles, `Canonical`, recombinant alleles), 13 (`vectors.jsonl`, `survival.json`, the `sv_` and `nn_` columns), 30 (T-NEURO-01…16 and the C6 fixture changes), 31 (S29–S33 and their variants).
- 16: check it against the code and the M16.7–M16.8 decisions (the worker task, `TickPool`, the Survival Report, `PlateauStop`).
- 20 §9: the performance budget, with the M16.8 measurement and the owner's decision to accept the miss.
- 40: new open questions, for example the Burst path and the Ollama mutator that the measured runs need.
- 01 glossary: the new terms (vector allele, ray fan, matched pair, plateau stop…).
- `README.md`: every chapter listed.

### 3. The status file and the log
Move the finished M16.1–M16.8 details from 50 to the end of 51. Keep in 50: the milestone rows, the next step, open decisions and live known problems. Under 200 lines.

### 4. The site
- `guides/`: every page whose topic M16 touched: senses-and-prompts (attachments, the ray fan), brains-and-decisions (non-text brains, the worker task), genes-and-mutation (vector alleles, crossover), recording-and-outputs (the new files and columns), testing (the new suites), determinism (one hash in any wait mode), editor-tooling (the menu, the Network panel, the Survival Report).
- `extending/`: `neural-species.md` checked against the code; sense.md, brain.md, genes.md, mutation.md, reproduction.md, recording.md mention what a student can now do (attachments, vector genes, a brain that doesn't read text) and link to `neural-species.md`; `extension-points.md` is generated: rebuild it, don't edit it.
- `index.md` and both `toc.yml`: every page reachable.

### 5. `///` comments
`audit.ps1 --only Neuro` and the folders M16.1 touched (`Runtime/Genes`, `Runtime/Brains`, `Runtime/Phases`, `Recording`, `Testing`). Fix what it reports. Check with `--same-code HEAD`.

### 6. Check and hand over
- The site build (Settings) and a read of the pages you changed in the built site.
- A last grep for the stale phrases of step 1.
- Tell the owner, in 10 lines at most: what changed (by area), what you could not check, what the code seems to get wrong (if anything), whether the site is ready to publish.
