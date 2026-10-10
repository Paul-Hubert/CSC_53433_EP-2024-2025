# P4 — Keep the contract in step with the prototype

From Docs/Interface/32-integrity-prompts-and-ci.md §3. Paste it into a Claude Code session at the root of the
Unity project; fill in the parts in <angle brackets> first.

---

The Python prototype (prototype/) changed since the commit the contract was written
against (see Docs/Interface/README.md, "Status"). List every behaviour change between
that commit and HEAD that touches a rule of Docs/Interface/ or a reference value of
14-configuration-reference.md. For each: the commit, the old and new behaviour, the
affected rule ids, and the proposed new wording. Ignore refactors that don't change
behaviour. Change nothing until I approve.
