## Entry 1959 — 2026-10-11 12:59 PDT — Train 1060: diagnostics name character positions, a source debugging line inside a literal is refused, LENGTH of bit-bearing variable groups computes, and the landing-pipeline redesign is recorded

Train 1060 is the land unit's train of four clusters, in `prune_worktrees.py --train` order: A PB2982, H PB2964, Z PB2426
and N PB3015. A, H and N were cut from `f91823991` and Z from `8399765be`. Origin/main `d65a23b16` did not move under the
landing lease, so the final rebase was a no-op. A and H gated leg 1 only (batched gating, PB2515). Z and N carry no code.
No cluster was ejected or dropped.

**A — PB2983 + PB2490 landed, PB2982 retired: one column unit for every diagnostic, and a source debugging line between
literal parts.** PB2983 reproduced: on `01 <two supplementary letters> PIC X(2) ZZZ.` the parser named `(5,26)` for ZZZ
in character position 24, because the parser counts UTF-16 units and the reference-format gates count positions
(§6.1 1) a), §6.3.1). Spans were in three different column units. `PhysicalLines` now keeps a `ColumnMap` (it was
`TabMap`, renamed in every caller; `ToExpanded` had no production caller and is deleted) for any line holding a tab or a
supplementary-plane character. `TextSite(Line, Index, Columns)` is the one place a diagnostic column (`Col(i)`) and an
origin span (`Spans`, split at each tab and each supplementary character) are made, for the fixed-form, tab,
continuation, free-form, copybook and `>>SOURCE FORMAT` indicator arms. PB2490 reproduced: a debugging line written
between the parts of a continued literal under WITH DEBUGGING MODE was accepted. §6.3.5 2) lets only comment and blank
lines stand there, and ISO 2023 has no debugging line, so rule 1's precedence goes to GnuCOBOL 3.2.0, which rejects it
under `-fdebugging-line`. The logical conversion now notes each such line, `DebuggingLineRewriter.Rewrite` names the
lines it keeps as source, and `Frontend` reports them as COBOLNET3473 (claimed; 3474-3475 returned) at the line's own
file and physical line, so a copybook's line is named in the copybook. PB2982's premise was false: a tab in positions
1-7 fills through position 8, so a non-space indicator is never tab-shifted, and a test pins column 7. Goldens:
negatives `pb2983-supplementary-letters-parser-error-column`, `pb2983-supplementary-letters-gate-error-column` and
`pb2490-debugging-line-between-literal-parts-source`, plus `DiagnosticPositionTests` facts and unit tests for
`PhysicalLines`, `LogicalLineBuilder` and `DebuggingLineRewriter`. No inventory row moved. The one train conflict was in
`DiagnosticPositionTests.cs`, where E's (train 1059) NIST test and A's new tests were added at the same place; both were
kept.

**H — PB2964 + PB2498 + PB2589: LENGTH / BYTE-LENGTH of groups holding USAGE BIT items, and the REPOSITORY FUNCTION entry
as a 2002 construct.** All three reproduced. A group with bit items and a dynamic leaf aborted with COBOLNET1756 at
LENGTH and BYTE-LENGTH. It now answers 3 for `A, B bit + D "ab"` (§15.50.4 7) c), §15.14.4 6)). `VariableLengthGroupSum`
is the one builder. Its fixed part is `ByteWidth` (already the §8.5.1.6.3 layout) minus each dynamic table's one
occurrence. An OCCURS DEPENDING table of bit elements is counted in bit positions through a `BoundOdoExtent` that now
carries `PositionsPerCharacter`: 8 rounds up to characters, and 1 gives a bit group's boolean positions (§15.50.4 1)).
The PB2498 lead that "only the one-way image gate may be needed" did not hold, because an ODO bit table's occurrences
share bytes. The ODO national group's LENGTH answered bytes (10 for 5 positions) and now answers positions (§15.50.4 2)).
PB2589 printed `03` at `--std 85` for a keyword-omitted function. The REPOSITORY FUNCTION entry is now the constructs
row `repository-function-2002`, gated in `VisitRepositoryEntry` (COBOLNET0900) and read by `KeywordOmittedFunction`
through the new `ConstructRegistry.IsAvailable` (which replaced a literal 2002). CONFORMANCE DOC-A.1-61 records the
dynamic-capacity bit-table determination. Goldens: `2014/pb2964_length_bit_variable_group`,
`2002/pb2498_length_odo_bit_national_group`, `2002/pb2589_repository_function_keyword_omitted` and the negative
`pb2589-repository-function-at-85`. No inventory row moved. The conflicts were whole elements of the 2002 and 2014
manifests, beside train 1059 D's goldens; both sides were kept and the counts checked (832 to 834, 236 to 237).

**Z — PB2426 (evidence only): ledger trend points through train 1059.** Eight points appended by the land units'
renders after trains 1058 and 1059 (`docs/rearchitecture/evidence/ledger-trend.json`, 219 points). PB2426 stays open.

**N — PB3015 (analysis, decided) + PB3016 + PB3022-PB3029 + PB1957: the landing-pipeline redesign.** The measured
Saturday (917 min, 6 trains, 38 clusters; 41 % of lander minutes inside local whole-population gates, 16.5 % waiting
for CI that already ran the population on 20 runners in 13 min) and the owner's four decisions of 2026-10-11: the
lander's local gate becomes the targeted leg, CI the regression net, landing through GitHub's merge queue, one PR per
finished branch, one DEVLOG file per entry. The design is `DESIGN-test-build-ci.md` §3.17, with pointers in §3.14.1,
§3.15 and `DESIGN-orchestrator-loop.md`. Implementation notes PB3022-PB3028 are open (campaign cluster PB3015), and
PB3016 closes with PB3023. Frozen evidence is under `evidence/fleet-optimization/`. The train's citation audit read
seven of N's design-section references (§3.17.x, §3.14.1) as phantom ISO clauses. Each is now prefixed with its design
doc in N's commit. Plan §0 records the decision.

**The train.** This train used the Q1a gate shape early, by operator steer (PB3015, PB3022): Windows
`build-local.ps1 -Mode implementer` leg 1 plus the whole WSL Linux gate, with CI the whole population. Windows run
`20261011T194653Z-1a2bb1`: `LEG 1 ONLY (batched gating, PB2515): GREEN`, Conformance 7,559/11,488, Unit
33,207/33,436, Characterization 36/36 run, with the rest left to CI. Linux GREEN on `3915e4ad9`: unit 33,469, characterization 36,
conformance 11,488, guard (NIST 362 MATCH, 2 TERMINATES declared), self-tests. The arch-oracle compare was DIFFERENT on
35 of 7,780 cases, all H's or new. 12 ADDED are the new goldens and the five `repository-function-2002` matrix rows. 23
DIAGNOSTICS are H's PB2589 gate, which adds a COBOLNET0900 for the REPOSITORY FUNCTION entry to each `@85` matrix
program and below-2002 negative that writes one (each was already rejected). A changed no existing case. The baseline
was re-recorded as `3915e4ad93af`. Semgrep counts are unchanged (268 · 3 · 44 · 2). The review of the merged diff found
no correctness defect. Three implementer leads became notes: PB3030 (a bit-bearing ODO group beside a runtime-length
table is still the COBOLNET1756 stage, `DataItem.CurrentExtentComposes`), PB3031 (columns after REPLACE-written text
holding supplementary characters drift, `OriginWriter`) and PB3032 (`test_orchestrate_stop.ps1`'s stale-lock case
failed under gate load and passes alone). GAP 103 → 103.

