# P2 — Review a student's extension

From Docs/Interface/32-integrity-prompts-and-ci.md §3. Paste it into a Claude Code session at the root of the
Unity project; fill in the parts in <angle brackets> first.

---

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
