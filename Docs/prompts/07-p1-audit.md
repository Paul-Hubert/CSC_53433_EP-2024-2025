# P1 — Audit the implementation against the contract

From Docs/Interface/32-integrity-prompts-and-ci.md §3. Paste it into a Claude Code session at the root of the
Unity project (CI runs it weekly, job agent-audit); fill in the parts in <angle brackets> first.

---

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
