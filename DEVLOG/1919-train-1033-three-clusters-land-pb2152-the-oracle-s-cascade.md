## Entry 1919 — 2026-10-07 11:17 PDT — Train 1033: three clusters land (PB2152 the oracle's cascade fix, B PB2076 universal returning MOVE/SET validity, C PB1353+PB1670 directive-in-pseudo-text and the partial-word gate); PB2463–PB2466 filed; GAP 158 → 156

The wave's final remainder: PB2152 (wave 1030 A; PB2151 split at the note boundary and left open), B (wave 1032 B)
and C (wave 1032 C), in that order, one commit each and this entry in the train commit.

**PB2152 — the oracle's first catch, and the regression it hid.** Trains 1028 and 1029 had landed 39 emitted-C# and
diagnostic differences green on every gate. Re-measured against a fresh capture of `f620ccf6d43d` the split was 31
CSHARP (15 corpus, 16 NIST), 4 DIAGNOSTICS and 4 ADDED: 29 whitespace-only re-renderings inside a subscript; pb136
routing a REF_LPAREN group through the expression binder (same value); pb17 making a UDF argument inside a subscript a
BY REFERENCE cell operand as everywhere else (§14.2.3 8)); pb1455's COBOLNET2776 moving onto `OF` (column 29, now pinned
in its `.err`). Three negatives were WORSE: a spurious second `COBOL0307 unexpected ')'`, because since PB2113 made the
reference paren its own token the integer inside `A(1)` resynced as a level-number. §8.3.5 4) makes a parenthesized
group one unit, so `CobolErrorStrategy.ConsumeUntil` consumes a group met while resyncing whole (stopping at the
separator period or end of file, the recovery set asked first) and `SingleTokenDeletion` never deletes a '('. Both
recovery arms are fixed; `ParseRecoveryParenGroupTests` pins the error count the fixtures' substring match cannot.
`compare_oracle.py --diff` now captures a recorded baseline's blobs itself in a detached worktree of that commit, and
refuses blobs that do not reproduce the recorded hashes. This lander used it for the first time: it captured
`ef2b7cb6250c` in 91 s and produced the per-case diffs this entry classifies.

**B — PB2076: §9.3.6 rules 6 and 7 ask VALIDITY, not only which statement.** The universal dispatch matched any
MOVE-class returning item to any other, so a method returning `PIC 9V99` matched an invocation returning `PIC X(4)`
(Table 16: No) and the INHERITS search stopped at a false match; the repro printed `EC-OO-UNIVERSAL` where §9.3.6 6)
gives `EC-OO-METHOD`. Table 16 (§14.9.25.3 SR10) and SR8 moved to the runtime as ONE copy, `MoveValidity`, which the
compiler's MOVE chain reads through `Table16Operand.Heading` and the run-time `ActivationRelations` reads through two
new description facts (`Table16`, `BinaryWidth`); `ReturningMatches` splits into `MayMove` (SR1, SR2, SR8, SR9, SR10)
and `MaySet` (SET's index, data-pointer, program-pointer and function-pointer rules). `MoveTable16.Refusal` is
deleted; `Table16PrintedTableDriftTests` holds all 81 modeled cells to the printed table. The lander re-read the new
table arm by arm against the deleted one: behavior-neutral for the compiler. Golden
`2002/pb2076_universal_returning_move_match` plus its below-2002 negative; 8 rows re-witnessed, all CONFORMS. Two
leads, both from the description lacking the type facts SET needs, are now notes: rule 6's object-class conditions
(PB2463) and prototype NAME vs signature identity (PB2464).

**C — PB1353 + PB1670.** A `>>SOURCE FORMAT` line inside COPY or REPLACE pseudo-text compiled, because logical
conversion discards it (§6.5 1)) and leaves a blank line the PB1384 question never saw. The merged driver now asks the
text's `ReferenceFormatMap` whether a blank line was a discarded directive and puts the same open-statement question
over the pending block without flushing it; COBOLNET2697 also cites §7.2.3.3 / §7.2.4.3 SR10 inside pseudo-text. The
note's `>>DEFINE` half did not hold on today's tree (already COBOLNET2697 since PB1384). PB1670 gates the
LEADING/TRAILING partial-word phrases at 2002 through one `OperandScreen.PartialWord` shared by COPY and REPLACE, once
per statement (provisional edge, VCR row 7.33). SR-7.2.3.3-10 and SR-7.2.4.3-10 close: GAP 158 → 156.

**The train.** Gate (lander mode, the whole population in one leg), run `20261007T180534Z-94acf0`: `=== BUILD-LOCAL
GATE: GREEN — Conformance 10,893/10,893 · Unit 31,720/31,720 · Characterization 35/35`; semgrep counts equal the
baseline. The one merge conflict was adjacent whole elements of `tests/conformance/2002/manifest.json` (B and C each
added a golden); both kept, 746 → 747. Oracle against `ef2b7cb6250c`: DIFFERENT, 110 of 7,418, every case attributed.
96 CSHARP are B's: the universal-dispatch `ActivationDescription` statics gain `Table16 = Table16Category.X` (and
`BinaryWidth = true` for a binary-width item), and nothing else changes (classified mechanically: removing those two
members gives back the baseline lines exactly). 3 DIAGNOSTICS are PB2152's (`l1c08-entry-name-subscripted`,
`l1c17-paren-not-delimiting`, `pb1372-cobol-words-undefined-picture-syntax-withdrawn` each lose only the spurious
`COBOL0307`). 11 are ADDED (B 2, C 9). The baseline is re-recorded on the train's tree as `e68c33651ec0`, and the
gated capture compares IDENTICAL to it. The review found no correctness defect; B's code comment and the OO design's
residue paragraph now name PB2463/PB2464. Wave 1032 A's two unfiled leads are now notes too: a constant's LENGTH OF /
BYTE-LENGTH OF is measured before description completion, a silent wrong value (PB2465); and a RENAMES THRU window
with an interior half-national byte is still refused (PB2466, PB1902's sibling). Nothing was dropped.

