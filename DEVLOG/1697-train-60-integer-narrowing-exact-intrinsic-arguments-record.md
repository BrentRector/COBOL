## Entry 1697 — 2026-09-24 23:59 PDT — Train 60: integer narrowing, exact intrinsic arguments, record extent tables, GR14 DEFINE scope (GAP 1458 → 1453)

Four wave-59 clusters in one landing, one commit each, on top of the register checkpoint cdb084309.

**w59b — PB1033 + PB1058 + PB1178 (narrowing half): one saturating narrowing for integer values.** Emit-time
`(int)`/`(long)` casts wrapped COBOL integer values past the host carrier, and binder `int.Parse`/`TryParse` sites
either crashed or dropped the value without a diagnostic. The re-probe confirmed every symptom in the notes:
`GO TO … DEPENDING` 4294967297 went to P1 where §14.9.17.4 GR2 says no transfer occurs. A ref-mod start of 4294967297
read position 1. `STOP RUN WITH ERROR STATUS 4294967296` exited 0. Literal ADVANCING, RETRY and TIMES counts failed
Roslyn with CS0221 or CS1021. PAGE LIMIT, COLUMN and LINAGE of 77777777777 crashed the compiler with an
OverflowException. The implementer found one more: OCCURS and RECORD/BLOCK CONTAINS 77777777777 compiled clean with no
diagnostic. The fix has three parts, one of each:
- one runtime narrowing, `CobolNum.Position` with a new int twin `Position32`, emitted only through
  `RuntimeApi.HostInt32/HostInt64`. The literal forms fold at compile time with the same functions.
- one binder reader, `IntegerOperandRules.HostValue`, which saturates and never throws. Sixteen parse sites were
  rerouted to it and `LineInteger` was deleted.
- one screen, COBOLNET2427 (the §4.5 implementation limit), with `FullValueSlots` exempt.

About twenty arms were rerouted. Among them, exit status is now clamped, not wrapped, and the runtime ref-mod range
test, which existed twice and overflowed near int.MaxValue, is now one overflow-safe `RefModOutOfRange`. Two semgrep
rules and `IntegerOperandSlotDriftTests` keep it that way. Tests: golden `2002/pb1033_integer_operand_full_value`,
negative `pb1058-integer-operand-beyond-limit`, and `StopGobackExitCodeTests.StatusBeyondTheHostExitCode_IsClampedNeverWrapped`.
Three characterization snapshots were re-baselined; the diffs are the intended ones. Verdicts: 8 rows (4 CONFORMS
closing the GAP, 4 PARTIAL). PB1033 and PB1058 are landed. PB1178 stays open with GR-14.9.42.4-5 closed; the ERROR-vs-value
question in the w59b report is already answered by the orchestrator decision on the note, and both paragraphs were kept
on the rebase. PB1163 now also claims GR-14.7.9.3-4. Self-review found no gaps in the fix. It did find missing witnesses
past the carrier for the LINAGE item, the ADVANCING item, B-SHIFT, RELATIVE KEY and CONTINUE AFTER. It also found three
note-ready leads, which are in the report: the report SUM/PAGE-COUNTER carrier is `long`, EC-BOUND-ODO is unverified
past a saturated ODO object, and an ALPHABET literal ordinal uses `int.TryParse`.

**w59d — PB1041 + PB1310: the intrinsics take the exact argument where the function is ill-conditioned.** The note's own
repros, such as `SIN(10**30)`, had already been fixed by PB999, but its rules had not. PB999 reduced only |x| ≥ 2π, so
SIN, COS and TAN were wrong at every multiple of π/2 below that: TAN of a 30-digit π/2 gave 1.63E16 where the true value
is 1.33E30. The re-probe found more of the same kind:
- LOG and LOG10 near 1: `LOG(1+1E−26)` gave 0.
- ACOS and ASIN near ±1.
- EXP and EXP10 at large |x|: about 150 ulps off.
- ANNUITY (PB1310): 1 + rate cancelled to 1.0, giving 0 where 0.08333333 is due.

`IntrinsicRenderer.WholeRangeBodies` is now the one set of the nine ill-conditioned members. Runtime `CobolDec` overloads
form each well-conditioned quantity exactly:
- `ReduceQuarterTurns`, a decimal Payne–Hanek reduction on Int128, replaces `ReduceTwoPi` and `PeriodicArg`.
- LOG and LOG10 use the exact x−1 with log1p.
- ACOS and ASIN use the exact 1−|x|.
- EXP and EXP10 use the exact fraction.
- ANNUITY is `rate / −expm1(−n·log1p rate)`.

Two goldens, `85/pb1041_trig_log_arc_exact_argument` and `2002/pb1041_exp_and_wide_exact_argument`, take their expected
values from a 140-digit oracle. The negative is `pb1041-exp-before-2002`. The pb253 SDIDI twins had TAN receivers sized
for the defective value, so they were widened to S9(31); their `.out` is unchanged. Verdicts: both batches re-applied in
order. Three `ReduceTwoPi` witnesses were retired and RV-15.9.4-1 went PARTIAL → CONFORMS. **A report claim that did not
hold:** the negative shipped without its `.err`, and the implementer's gate ran only `Manifest_CoversEveryProgram`. The
landing gate's whole-assembly leg failed `CorpusRunnerTests_P0.EnabledNegativeCase_RejectsWithItsDiagnostic("pb1041-exp-before-2002")`
with FileNotFound. The lander added the `.err` with COBOLNET1502, the code the report names. `cobol --std 85` emits it at
(13,12), and the re-run negative leg passed 1579/1579. The implementer reported two leads: VARIANCE and
STANDARD-DEVIATION cancel narrowed arguments, and EXP overflows binary64 inside the SDIDI range.

**w59e — PB1053: variable-length group records carry their extent table.** The re-probe held, and the defect is wider
than the note says: any record with two or more dynamic-length components was not invertible. `AA`/`KEY`/`CCCC` read
back as `AAKEY`/`CCC`/`C`. Under determination D-FRA (v) each such record now travels with its extent table as framing
outside the record. §9.1.7.2 licenses the implementor information, and §12.4.5.11.4 GR1 keeps it out of the record
area. The table is honoured only by a corresponding group (§8.5.1.12.2), and only when it describes the record
received; otherwise the take step still applies. The structure is one of each:
- one type, `RecordExtents`
- one codec, `RecordFraming`, marked by bit 31 of the length word, with `StoredFrame` shared by the keyed and sort stores
- one decomposition, `CobolContiguousLayout`

Both arms are fixed: the send arm is `OperandText.RecordAreaExtents` and the receive arm is `EmitRecordAreaStore(…, currentExtents)`.
The sibling sweep moved the PB1025 key locators, `IndexedConnector.KeyOf/AreaKey` and `CobolSort.Key.At`, onto the
table. Tests: golden `2014/pb1053_dynamic_members_both_sides`, which covers sequential, relative, indexed with the key
flanked by dynamic members, and SORT; negative `pb1053-dynamic-members-both-sides-at-2002`; and `RecordExtentsTests`.
No rows (closes_rows [] with a reason). **Lander merge:** `CobolFile.cs` and `FileRegistry.cs` conflicted with w59b on
the same six signatures. w59b had widened `retryAmount` from int to long, and w59e had appended the extents parameter.
The resolution keeps both changes. The implementer reported four leads:
- Format-1 `RECORD CONTAINS` with a variable group reads back wrong.
- LINE SEQUENTIAL keeps the take step (an owner call).
- `GrammarDiagramGeneratorDriftTests` shares a fixed temporary directory; this is PB1564.
- An indentation error in the §14.9.40.3 SR6 transcription.

**w59f — PB1066 (partial): the GR14 implicit PUSH ALL / POP ALL now reaches >>DEFINE.** The note held: a >>DEFINE in a
WHEN or FINALLY phrase outlived END-PERFORM, and the probe printed `ZZ-LEAKED`. The cause was that the binder placed the
ops after the conditional-compilation driver had already run. Now the front end places them once:
`ExceptionPerformDirectiveScope` moved to `Frontend/Preprocessor`, and `PerformFormat.IsFormat3` is the one
discriminator. `Frontend.Parse` is now a fixed point. The driver reports each directive encounter, each op is keyed to
the next encounter, and the driver re-runs until the result is stable. Line numbers stay exact across COPY splices and
REPLACE (`ReplaceLineMap`), and ordinary sources converge on the first pass. Tests: golden
`2023/pb1066_gr14_implicit_pop_all_define` and negative `pb1066-perform-when-define-below-2023`. The SOURCE FORMAT arm,
held by the normalizer, is not fixed, so PB1066 stays open and GR-14.9.28.4-14 stays PARTIAL. Its batch was re-applied
to the base row, retiring the two moved witnesses.

**The train.** The four patches were taken from the implementer branches against base df452130e. Inventory hunks were
excluded, and the five verdict batches were re-applied with `record_verdicts.py` on the merged tree. The only code
conflict was w59b × w59e, described above. Rebasing onto cdb084309 conflicted in `kb/Work/PB1178.md`, and both
paragraphs were kept. Checks:
- Twelve report citations were re-run through `cite.py --check`, three per cluster, and all were OK.
- Only COBOLNET2427 was claimed, in w59b's range, so there is no cross-cluster collision.
- Semgrep verify: PASS before and after, with no count increase (BigInteger 46, decimal 2).

The landing gate ran the whole Conformance assembly (`~CobolNet.Tests`, which selects every class; the lander's first
filter spelled the namespace `Cobol.Net.Tests`, and filter_population refused it as DEAD):
- Conformance: 8688/8689. The one failure is the w59d `.err` above; after it was added, the negative leg passed 1579/1579.
- Unit: 29267/29267.
- Characterization: 33/33.
- Legacy Integration: 503/504, with 1 skipped.

`=== CITATIONS: RED ===` is the 14 pre-existing `audit_code_citations` findings (kb/Work PB1080, PB1140, PB1144, PB1192,
PB1301, PB1364, PB1418, and the two l1c01 goldens). None of those files is in the train. GAP 1458 → 1453. No cluster was
dropped.

