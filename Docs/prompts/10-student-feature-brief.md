Write a **short document that prepares students to implement a new feature in EvoSim**: what to read, which mechanism to use, how to start, the rules to keep, and the mistakes to avoid, for each kind of feature they may be given. It is a briefing for the first lab session, not a tutorial: the existing site pages already teach each mechanism, and this document points to them. One session should be enough.

Fill in **Features** first, or keep the default list.

## Settings

| Setting | Value |
|---|---|
| Branch | `llm-evolution`. `git pull` first; never force-push. |
| Output | One site page: `Docs/Site/extending/before-you-start.md` (front matter `uid: extend-before-you-start`), linked first in `Docs/Site/extending/toc.yml` and from `extending/index.md` and `index.md`. 150–250 lines: a little longer than the usual page limit, because it covers every kind of feature. |
| Readers | Second-year students who know C# and a little Unity, and are new to EvoSim and to evolutionary simulation. |
| Unity | Not needed. Don't start it: an M16.9 run may hold the project. |
| Commit | `EvoSim docs: a briefing before students implement a feature`, with the attribution lines your session requires; push (pull first). |

## Features

The kinds of feature students may be asked to build. Replace this list with the course's own if the owner gives one:

<FEATURES — default below>

1. A **sense** (06; recipes 22 §2 and §13): something an animal perceives, as tokens the brain reads.
2. An **action** (07; 22 §3 and §11): something an animal does, as an intent the act phase applies.
3. A **stat and a death rule** (05; 22 §1): a quantity that rises and falls, and what happens at its limit.
4. A **gene**: a number (22 §4), a free text (22 §5), or a vector (16, the neural species).
5. A **mutation operator** (10; 22 §8).
6. A **brain** (08; 22 §7): text and a language model, or numbers and a network (16).
7. A **resource layer or environment change** (03; 22 §6, §9): food, cover, seasons, ground.
8. A **new species** (04; 22 §10), including a neural one built from the menu (16).

## Context (read before writing)

1. `Docs/Site/extending/index.md`, `extension-points.md` (generated: the list of base classes), `exercises.md` (the teaching path), `testing-your-module.md`, `worked-example-alarm-calls.md`, `neural-species.md`.
2. `Docs/Interface/22-extending-recipes.md` (the recipes) and `30-tests.md` §4 (the conformance suites).
3. `Docs/Interface/32-integrity-prompts-and-ci.md` §3 P2: the rules that matter most for a student's module. Use them as the list of rules to keep, not as a review: the lab is free exploration, with no hand-in and no grade.
4. The rules students break most often, with their ids: no per-animal state in a module (CORE-05), no global randomness (RAND-01), actions only produce intents (ACT-06), a sense returns declared tokens (SENSE-01) and keeps the observation space small (SENSE-05), prompts under the brain's limit (PROMPT-06). Check each id in `Docs/Interface` before you cite it.
5. `Docs/Site/README.md` § Writing a page: the site's style.

## What the page contains

1. **What a feature is in EvoSim** (5–10 lines): everything is a component under the World or a species; the World discovers it, validates it and runs it in a fixed tick order (link the-tick guide). Your class derives from a public base class, exactly like the reference modules.
2. **Before you write code** (a short checklist):
   - say in one sentence what your feature changes in an animal's life, and which species it applies to;
   - choose the mechanism with the table below;
   - find the closest reference module and read it (the table names it);
   - decide how you will see that it works: a stat column, a view, the Animal Inspector, a scenario.
3. **One table, one row per feature kind** of **Features**. Columns: the base class (an xref), the recipe (22 §n), the reference module to read, the conformance suite that will run on it, the site page, and one line of "watch out". Every name is checked in the code.
4. **The workflow in five steps**: *Assets ▸ Create ▸ EvoSim ▸ Script* (or derive by hand); place the component; Play and read the validation messages; run the conformance suite of its kind; record a short run and look at `stats.csv`. Link the pages; don't repeat them.
5. **The rules every module keeps**, as a short list with rule ids, each with one sentence of why and one of how to keep it. Plus determinism: same seed, same run (link the determinism guide).
6. **Common mistakes** (5–8): concrete ones from the recipes, the exercises and the alarm-calls example. Examples: keeping a `List<Animal>` field in a module; calling `UnityEngine.Random`; moving an animal from an action; a sense with dozens of tokens; forgetting to add a new prey to the predators' diets (V-79); a brain that ignores the attachments it says it accepts.
7. **Watching your idea at work**: the editor windows that show what a module does, and changing one thing at a time with the same seed. No section on asking for help, handing in or grading: the lab is free exploration, and the page is documentation for creative ideas.

## Rules

1. **Short and pointing.** Each section links to the page that teaches its topic; the briefing never re-teaches a mechanism. If a needed page doesn't exist, say so to the owner instead of writing it here.
2. **Accurate.** Every class, menu path, file, rule id and recipe number is checked in the code or the contract. No invented values.
3. **Plain English**, short sentences, tables for comparisons, numbered lists for procedures. At most one small diagram (```mermaid), if it helps more than a sentence.
4. **No code longer than ~15 lines**; a ```csharp block that declares a type must compile against the current API (the site build compiles it), otherwise mark it ```cs.
5. If DocFX is installed, build with `powershell -File Docs\Site\build.ps1 -Strict` and read the page in `_site/`; otherwise run `Docs\Site\tools\audit.ps1` and say in the commit that the strict build was not run.

## Hand over

Tell the owner, in 8 lines at most: the page's path and length, the feature kinds it covers, anything you could not check, and pages that seem to be missing or out of date (for the docs pass, `Docs/prompts/09-update-documentation.md`).
