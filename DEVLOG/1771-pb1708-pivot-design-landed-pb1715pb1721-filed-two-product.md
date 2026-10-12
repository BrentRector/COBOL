## Entry 1771 — 2026-09-28 21:10 PDT — PB1708 pivot design landed; PB1715–PB1721 filed (two product speed defects, five gate mechanisms)

The design workflow wf_08642598-d3c finished in three steps: the architect wrote Entry 1769, the adversarial review
returned APPROVE_WITH_CHANGES with 2 BLOCKING and 9 NON_BLOCKING findings, and the reviser resolved all eleven
(Entry 1770).

**The merge.** The reviser's branch is merged into main, bringing `DESIGN-test-build-ci.md` sections 3.13 and 3.14
and the evidence in `docs/rearchitecture/evidence/impact-map-pb1708/`. The DEVLOG conflict was resolved by
renumbering the branch's entries above main's 1768 (to 1769 and 1770), byte-safe with CRLF preserved.

**The orchestrator filed the notes the design could not** (agents do not allocate ids):
- **PB1715:** the lexer never caches its default mode's start state, because of six left-edge predicates. Every token
  in every `cobol` compile pays ATN simulation, and parallel compiles serialize on `lock(dfa.states)`.
- **PB1716:** `TableValueOdometer.Resolve` fills up to 64M elements before SR23 rejects the entry, so a 26-line
  erroneous program takes 29.6 s to be rejected; it is the suite's 219 s long pole. SR23's clause was re-derived
  mechanically: `cite.py --check 13.18.63.3` passes.
- **PB1717–PB1721:** the gate mechanisms.
  - M11, the order plan; closing it also closes PB1712.
  - M14, one population tool.
  - M12, the in-assembly leg filter.
  - M2, the FIFO gate cap.
  - M13, the ordered gate, which deletes the selection interface together with every caller.
  - Their `blocked_by` fields carry the dependencies.

**Two frictions:**
1. The first draft of the notes cited design-doc sections with `§`, which is the same phantom Entry 1766 fixed.
   The citation audit caught all nine before commit.
2. The register has no harm flag for compile-time PERFORMANCE, so PB1715 and PB1716 are `process_only: true` with
   the reason stated in each note, even though both are product defects shipped in the CLI.

