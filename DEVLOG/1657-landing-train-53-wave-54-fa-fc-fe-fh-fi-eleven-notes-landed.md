## Entry 1657 — 2026-09-23 00:29 PDT — Landing train 53: wave 54 (FA, FC, FE, FH, FI), eleven notes landed, PB1006 retired, PB999 dropped, GAP −13 to 2101

Train 53 was pipelined behind train 52 (workstream SKILL.md "Lander throughput"). Four clusters (FA, FC, FE, FI) were
merged and gated on `acff7ee45` while trains 51 and 52 were still in flight. The fifth, FH, was held back: its branch
had merged train 52's DB and DD branches, which it builds on (PB1023 adds a row to DB's `OperandClassScreen`, and
PB1021 reuses DD's `addressIdentifier` rule). The lander waited about 50 minutes for "Train 52" to reach origin/main. It
then rebased (conflicts only in `docs/DIAGNOSTICS.md` and one corpus manifest, both whole-row keep-both), re-applied
the verdict batches to main's inventory (identical to the text merge), brought FH in as ONLY its own work
(`8a645fada..2be488603` — the merges and the post-merge fix belong to train 52, and were already on main), and ran
the whole gate a second time.

**FA — directive state (PB1004 + PB1005; PB1006 retired).** ISO §14.9.28.4 GR14 brackets an exception-checking
PERFORM's imperative statement with an implicit PUSH ALL + TURN OFF ALL and, "Immediately preceding the END PERFORM
phrase", an implicit POP ALL. Before the fix, a `>>TURN` or `>>REF-MOD-ZERO-LENGTH` inside a WHEN phrase leaked past
END-PERFORM. The implicit pair is now replayed through PB941's directive-state stack
(`ExceptionPerformDirectiveScope`), and the witness for GR-7.3.20.4-1 moved from `CarrierFor` to `Replay`. PB1005:
§7.3.22.3 SR3 / §7.3.20.3 SR3 is now a §4.2.2 warning, COBOLNET2344, from `PushPopAllPlacementPass`. PB1006 is RETIRED
because §7.2.1 Step 1 REQUIRES a SOURCE FORMAT directive in the false path to be processed. Its two §7.3.16.4 rows are
CONFORMS on a golden that pins them.

**FC — ACCEPT and INVOKE BY CONTENT through the MOVE chain (PB1013 + PB1007).** ACCEPT format 1 sized its device
window from the PICTURE, so an ANY LENGTH receiver took one character. `ReceivingStore.CharacterPositions` is now the
one run-time receiver size (§14.9.1.4 GR3/GR4, §13.18.2.4 GR1 b)). The §15.4 function temporary moved as its 30-digit
implementor description, so `MOVE FUNCTION INTEGER(N) TO A B` stored `0000` twice. That was a wrong answer on main
that the note had only suspected. The temporary is now scale 0 for INTEGER and flagged, so the one text reader renders
the literal form. PB923's expression-arm guard fell with it: INVOKE BY CONTENT crosses by Table 16 (§14.8.2.3.3
rule 2 d)).

**FE — report LINE clause (PB1001 + PB1002).** `LINE … ON NEXT PAGE` was staged loud (COBOLNET0899). It is now a flag
on the group's first report line. A body group's page fit fails (§13.18.35.4 GR4a), and a report footing starts its
own page (GR5a). COBOLNET2199 now screens SR3, SR5 ("If the report is not divided into pages, all its LINE clauses
shall be relative"), SR7 and SR8. ⚠ THE WHOLE-ASSEMBLY GATE FOUND WHAT NO IMPLEMENTER TERM COULD:
`ReservedWordPositionConformanceTests.ReportGroupColumnClause_KeywordUse_NoReservedWordDiagnostic` was red at all four
editions. Its fixture used an unpaged RD with an absolute `LINE 1`, which SR5 now correctly refuses. The test is about
the COLUMN keyword, so its RD now carries `PAGE LIMIT 60`. This is a non-conforming fixture made conforming, not
valid COBOL edited to dodge a bug.

**FH — operand surfaces (PB1021 + PB1023 + PB1017).** The address-identifier is now a relation operand and an INVOKE
argument in every phrase (§14.9.23.3 SR9/SR19). It goes through PB239's one `addressIdentifier` rule and a new
`BoundAddressOperand`, and the class-pointer verdict moved to `PtrBinder`, so CALL and INVOKE share it. The WRITE
ADVANCING count is screened on both arms (§14.9.51.3 SR14/SR15, COBOLNET2365). REPOSITORY §12.3.8.3 SR1 compares the
whole specification, keyed on the name.

**FI — small residue (PB1016 + PB1015; PB999 DROPPED).** CONSTANT AS LENGTH OF now reads its operand through the one
`ReadWritten` decomposition and the one §8.4.2.3.3 SR2/SR3/SR5 screen. The THROUGH range's IN alphabet-name phrase is
gated at 2002. ⛔ **PB999 was dropped at the merge.** Its branch was cut before PB952 landed. PB952 made
`RuntimeApi.DomainArg` the ONE argument-domain screen and removed the double bodies' own tests. PB999 adds `CobolDec`
LOG/LOG10/SQRT overloads that screen the domain themselves, plus a renderer arm ahead of `DomainArg`, and it restores
the double-body tests. `git apply -3` conflicted in exactly those two files. The value defect (10⁻⁴⁰⁰ → +0.0 → −∞) is
still real on main, because `DomainDec` hands the body the NARROWED double. The fix needs a screen that returns the
unnarrowed carrier, which is a design choice, not a merge. PB999 stays open, and the conflict is written into its note.

**The train.** Gate 1 (`acff7ee45` + FA/FC/FE/FI) ran the whole Conformance assembly: `Failed: 4, Passed: 8165`, all
four reds the FE fixture above. After the fix: Unit `Passed: 28895`, Characterization 33, legacy integration
`Passed: 503, Skipped: 1`. Gate 3 ran on the rebased tree with FH: `=== WAVE-LOCAL GATE: GREEN (filter
FullyQualifiedName~CobolNet.Tests) ===`, Conformance `Failed: 0, Passed: 8246, Total: 8246`, Unit
`Failed: 0, Passed: 28952`, Characterization `Failed: 0, Passed: 33`, legacy integration `Passed: 503, Skipped: 1`.
The GnuCOBOL corpus fetched, so the population drift pair is a real pass. Semgrep did not move (verify PASS).
**GAP 2114 → 2101** (−13: FA/FC/FE/FI −9, FH −4). Codes claimed: COBOLNET2344 (FA) and 2365 (FH).

