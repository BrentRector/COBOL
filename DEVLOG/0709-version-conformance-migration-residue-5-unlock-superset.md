## Entry 709 — 2026-07-08 21:22 PDT — Version-conformance migration, residue #5: UNLOCK → superset parse + bind-time gate (guesser arm deleted)

First construct of the RESIDUE-FIRST migration (Entry 708 / DESIGN-version-conformance-pipeline.md §4/§5 Batch A).
UNLOCK (ISO §14.9.47, COBOL-2002) moved off the parse-time predicate + reverse-signature guesser onto the single
bind-time mechanism, with the construct's identity known at gating:
  - Grammar: dropped `{is2002()}?` from the `unlockStatement` statement alternative (`CobolParserCore.g4`) — UNLOCK now
    parses at ALL editions (superset). ANTLR regen.
  - Bind: `BindUnlock` (`StatementBinder.FileLock.cs`) now calls
    `ConstructRegistry.Check(edition, sink, Constructs.UnlockStatement2002, "the UNLOCK statement")` at entry (before the
    file-resolution check), so a below-2002 UNLOCK yields an exact COBOLNET0900 — mirrors the sibling record-lock gate at
    line 45 and the `DeleteFile2023` precedent.
  - Guesser: deleted the `CobolLexer.UNLOCK => Constructs.UnlockStatement2002` arm from `ReservedWordEditionHints`.
  - Fixture: `tests/conformance/negative/unlock_below_2002.cob` (reject-at 85 → COBOLNET0900); manifest 48→49 enabled.
Verified empirically: real UNLOCK @85 → exact bind-time 0900; @2002 → no 0900; the DEVLOG-708 mis-fire is GONE
(`PERFORM UNLOCK UNLOCK.` @85 → neutral COBOL0001, no spurious 0900); and — the superset-safety check — a data item /
paragraph / MOVE·DISPLAY·PERFORM operand named UNLOCK compiles CLEAN @85 (unchanged), while the §8.9 funnel still
rejects the user-word UNLOCK @2002 (COBOLNET0901). No 85 mis-parse. Battery: greenfield conformance **3109** (+1
fixture) · unit **227** GREEN; FULL legacy guard **ALL GREEN** (shared grammar — the legacy compiler shares the
frontend); characterization gate green. Grammar change was pre-authorized (owner 2026-07-05) + carried the full legacy
guard. 6 residue constructs remain (PROPERTY, PD-RAISING, XOR next in Batch A; then SHARING/RETRY/boolean in B/C).

