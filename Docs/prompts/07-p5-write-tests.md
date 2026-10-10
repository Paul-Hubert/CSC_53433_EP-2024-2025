# P5 — Write the tests for a rule or a module

From Docs/Interface/32-integrity-prompts-and-ci.md §3. Paste it into a Claude Code session at the root of the
Unity project; fill in the parts in <angle brackets> first.

---

Write tests for <rule id(s) | module class>, following Docs/Interface/30-tests.md: the
naming (T-<AREA>-NN), the helpers of EvoSim.Testing (WorldBuilder, ScriptedBrain,
FakeMutator, Place, Stat, Golden), fixed seeds, statistical asserts within 4 standard
errors, and a citation of the rules in each test's description. Add each new test to the
list in 30-tests.md. Run the tests and show that each fails when the behaviour is broken
on purpose (then restore it).
