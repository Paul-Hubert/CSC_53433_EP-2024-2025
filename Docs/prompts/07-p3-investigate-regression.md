# P3 — Investigate a regression

From Docs/Interface/32-integrity-prompts-and-ci.md §3. Paste it into a Claude Code session at the root of the
Unity project; fill in the parts in <angle brackets> first.

---

A regression test failed: <R-01 pinned hash for <scene>/<seed> | R-02 range <metric>>.
Find why, without changing the pinned values.

1. Reproduce it locally; record the old and new values.
2. Find the first tick and the first event where the two runs differ (dump both events
   files and compare); name the phase and the module that wrote it.
3. Find the commit that introduced it (bisect if needed).
4. Classify: an intended behaviour change (then say which rules and documents must change
   and propose the re-pin with its justification), or a bug (then propose a fix and a test
   that would have caught it).
Deliver a short report with the evidence.
