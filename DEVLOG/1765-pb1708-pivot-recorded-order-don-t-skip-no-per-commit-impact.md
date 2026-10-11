## Entry 1765 — 2026-09-28 20:35 PDT — PB1708 pivot recorded ("order, don't skip"); no per-commit impact maps; CLAUDE.md Testing corrected; PB1712 filed

**The first PB1708 design was rejected twice.**
- The first review REJECTED the design (7 BLOCKING findings). A reviser closed 5.
- An independent re-review REJECTED it again: 2 still open and 3 new BLOCKING.
- Each cycle found another way the single-process, memoizing compiler hides dependencies from the impact map:
  values memoized into static collections (`CobolKeywordTokens.Probed`, `CollationKeyCache`); edition state cached
  in the preprocessor, lexer and parser.
- Any such hole lets precise selection SILENTLY skip a failing test. Even perfect method-level precision has a
  median of about 69 % of the suite.
- The re-review also found that the source-generator reversal (the owner's third decision, given on the
  orchestrator's framing) rested on a false premise and would create two mechanisms for one job. That approval is
  WITHDRAWN.

**The owner's decisions (all in PB1708):**
- "Pivot: order, don't skip". Every gate runs the whole population ORDERED by impact likelihood; the map only orders
  and never skips.
- Invest in whole-suite speed; keep the gate cap until it is measured out; shelve the registry restructuring as a
  speed measure.
- "Don't record an impact map on every main commit". PB1709 is retired, since a stale map only mis-orders tests.
- "Edit the CLAUDE.MD". Its Testing section had said "run only the WAVE-LOCAL filtered gate (~2 min)", stale since
  train 70b. It now states the derived gate, the lander's whole-suite gate, the battery in its own worktree and the
  ORDER, DON'T SKIP direction. It gets updated again when the ordered gate lands.

**PB1712 is filed.** The impact map is blind to static-field reads and can silently under-select; for example, a
change to one DiagnosticCatalog entry selects 80 tests while up to 9,225 reach its readers. It is harmless under
ordering-only, and it stays documented.

**The pivot design** (workflow wf_08642598-d3c) is written and under adversarial review. It measured the Conformance
run at about 571 s on the critical path. The continuity partitions are 525–557 s each, and each testhost uses about
3.8 of 24 cores. It found two PRODUCT speed defects:
- M6: the ANTLR lexer re-computes its start state per token, which the `cobol` CLI pays on every compile;
- M7: a table VALUE fill that checks §13.18.63.3 SR23 late and over-fills; this is the 219 s long pole.
The plan's mechanisms are M6, M7, M11 (the order plan), M12 (in-assembly leg filters and orderers), M2 (the gate
cap) and M13 (the ordered whole-population gate).

