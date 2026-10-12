## Entry 1379 — 2026-08-29 07:17 PDT — PB137 LANDED: COMMIT/ROLLBACK get their identity — the ONE cobolWord reservation predicate ends the operand-list absorption, and the bound node makes SR1/SR2 enforceable

The absorption had one root: COMMIT and ROLLBACK were unconditional cobolWord alternatives, so every open
operand list — DISPLAY's tail, the arithmetic receiver lists, EVALUATE's WHEN objects — greedily absorbed
a following bare facility verb and ReportUnidentified rejected legal COBOL-2023 (`DISPLAY "OK" COMMIT`
inside IF drew COBOLNET1639 where GR1 requires CONTINUE-equivalence). The fix is ONE left-edge predicate
per word: the cobolWord alternatives are gated on §8.9 NOT reserving the word at the compile edition — at
2023 no list can absorb them and the statement arms parse; below 2023 both stay ordinary user words, which
is itself the §8.9 rule (probed: a data item named COMMIT compiles and displays at --std 2014). This also
closed CONFORMANCE.md §1's documented warning dead spot (the EVALUATE WHEN arm) — the callout is
rewritten to the current state.

The identity: BindCommitRollback produces BoundCommitRollback (the §4.2.6 named warning 1579 unchanged —
the facility itself stays the documented A.3 items-6/7 non-support, owner-owned per A.4.3), which enables
what the payload-free BoundNop made impossible forever: §14.9.7.3/§14.9.36.3 SR1 rejects the statement in
a RECURSIVE source element at bind (explicit, GR4-inherited, and the always-recursive functions/methods —
COBOLNET1690), and SR2's SORT/MERGE-procedure ban rides the SAME prohibited-range cross-pass that
implemented exactly this rule with a MERGE-only predicate (the batch-8 finding verbatim). GR1's
CONTINUE-equivalence is pinned by the golden across all three formerly-unparseable positions.

THE GATE'S TWO REDS, both the fix's own edges: the predicate came out of a GENERATED file, so it moved
into gen-cobol-words.ps1 behind a reservationGated flag in cobol-words.json (the drift test's extractor
learned the predicated form); and the DECLARATION path lost its targeted §8.9 diagnostic — dataName gained
predicated re-admission alternatives and the ParseArm a VisitDataName funnel arm, so `01 COMMIT PIC 9`
draws COBOLNET0901 naming the word (the user-word-commit pin re-shaped to the declaration screen — the
REFERENCE position is now structurally unparseable at 2023, a stronger rejection than the old funnel's).

Verdicts: SR-14.9.7.3-1/-2 → CONFORMS, GR-14.9.7.4-1 → CONFORMS, GR-14.9.7.4-2 → PARTIAL (the dynamic
under-the-control-of EC half stays with the A.3 posture); GR-3/-4/-5 (the transaction semantics) remain
NOT-IMPLEMENTED under the documented non-support, the owner's A.4.3 question.

