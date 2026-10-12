## Entry 145 — 2026-03-24: Full NIST Sweep — Guard Suite 33→55

Ran all 95 NIST NC-series test programs end-to-end: compile, run with 10s timeout, compare
against expected output. Results: 52 pass at 100%, 26 compile failures, 15 compile+run with
no expected baseline, 2 runtime hangs.

**The embarrassing discovery**: 19 tests were already passing at 100% but weren't in the guard
suite because nobody had generated expected output files. These tests were silently passing
on every build — we just never checked. Adding them required zero code changes.

Three more tests (NC231A, NC242A, NC243A) also passed 100% but had never been in any test
list. Total: 22 tests added, guard goes from 33 to 55.

**Lesson**: always run the full suite after a batch of fixes. Individual test-by-test work
creates tunnel vision. The full sweep revealed we were significantly further along than the
guard count suggested — 55 of 95, not 33 of 95.

The sweep also produced a prioritized blocker list. Biggest wins remaining:
- CBL2605 DIVIDE REMAINDER validation too strict (2 tests, one-line fix)
- CBL0901 MOVE validation too strict (1 test, one-line fix)
- BLANK WHEN ZERO grammar (2 tests)
- INSPECT TALLYING/REPLACING (NC223A, 42 of 94 fail — biggest single-test impact)
- STRING WITH POINTER (NC217A)
- PERFORM WITH TEST BEFORE/AFTER (NC204M)

---

