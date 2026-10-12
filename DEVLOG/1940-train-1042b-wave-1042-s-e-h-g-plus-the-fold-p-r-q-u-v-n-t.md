## Entry 1940 — 2026-10-08 11:56 PDT — Train 1042b: wave 1042's E, H, G plus the fold P, R, Q, U, V, N, T — intrinsic and exponent exactness, one edit mask fact, float-edited ROUNDED stores, MinValue magnitudes, wide subscripts, 32 owner-signed derivations, SORT/MERGE uncovered fatal OPEN, the DEVLOG rule at the landing, dead artifacts (PB2630, PB2632, PB2633, PB648, PB2638, PB2639, PB2617, PB2643, PB2616, PB2700, PB2605, PB2230, PB2233, PB2234, PB2193, PB2702, PB2703 landed; PB2231, PB2232 retired; PB1636 half; PB2694–PB2698, PB2702 filed)

**E — PB2631, PB2630, PB2632, PB2633, PB648 (PB2631 stays open on its §15.4 maximum arm).** Five intrinsic arms of
PB2631: a NUMVAL-C grouping separator now needs a following digit (`NvScan`'s pending group; the locale arm already
looked ahead), ANYCASE is ONE fold (`CobolLocale.LowerCase`) under the module's CHARACTER CLASSIFICATION in NvScan,
FindString and Substitute, STANDARD-COMPARE's omitted level is a null `long?` rather than 0, and
SECONDS-FROM-FORMATTED-TIME computes in Int128. PB2630: the SDIDI INTEGER / INTEGER-PART / FRACTION-PART bodies (and the
MOD/REM fallback through them) take the one exact integer-part rule (`CobolDec.TruncateToInteger`/`FloorToInteger`;
the duplicate `CtNumeric.IntegerPart` is deleted). PB2632: `** N` with a base of 0, 1 or -1 is O(1). PB2633 and PB648:
`FromDoubleBounded` handles a trailing-P (negative) scale, and `Pow10.AsWide`/`FiveAsWide` throw outside their tables
instead of answering silently. That throw exposed two more defects, both fixed in the cluster: `CobolDec.Div` returned a
quotient 10x too large for a 39-digit numerator, and trailing-P seconds arguments of FORMATTED-TIME/-DATETIME,
COMBINED-DATETIME and LOCALE-TIME-FROM-SECONDS gave silently wrong times (`CobolDate.WholeSecondsAtScale`). Nine
goldens. PB2631's §15.4 maximum arm (CONCAT, SUBSTITUTE, UPPER/LOWER-CASE and REVERSE past 8,191 positions) needs an
owner decision, recorded in the note.

**H — PB2639, PB2638 (landed).** PB2639: the digit-position rule of §13.18.40.5 rule 6 was written in five places and
three of them dropped the `EditRule[]` that makes a floating character-1 a digit position, so `LLLL9.99 EDITING L FOR
NEGATIVE IS "DEBIT "` refused 10 ON SIZE ERROR and `LLL.LL` rendered 1.5 as `   .01`. It is now one type,
`CobolEdit.MaskFacts`, that Render, DeEdit, MaskCapacity, MaskScale and TryFormat all read; the last two take the edits
as a required argument. The note's "also found" lead fixed the Int128.MinValue magnitude at every unsigned-receiver and
capacity site (TryStore, WrapBinary, InBinaryRange, PositionOf, RescaleStoreCap, InPictureRange, CobolEdit,
CobolLocaleEdit): a signed 16-byte COMP-5 holding -2^127 now stores 2^127 into an unsigned one, and StoreOrRaise raises
the size error instead of OverflowException. PB2638: a store into a floating-point numeric-edited receiver ignored
ROUNDED [MODE]. `FormatFloatCore` now rounds through the one `CobolNum.RoundDiv` kernel, renormalizes a carry, and tests
the exponent after it (§14.7.5 case 3); PROHIBITED with a tail is Inexact (§14.7.4.3 rule 7). `FormatFloatMove` became
`FormatFloatStore(..., mode)`. The sweep also fixed the outermost quotient into a float-edited receiver (1 / 3E24 was
+0.00E+00; it is now formed on the SDIDI by round-to-odd). Goldens `2023/pb2639_floating_char1_digit_positions`,
`2023/pb2639_binary_minvalue_unsigned_magnitude`, `2023/pb2638_float_edited_rounded_store`. Batch
`w1042h/batch.json` re-sites GR-14.6.8.4-1 and GR-14.9.25.4-6 to `FormatFloatStore` and adds witnesses; no GAP moved.

**G — PB2617, PB2643, PB2616 (landed).** Native `**` of a scaled or SDIDI base to an integer exponent is exact
(`CobolWide.Pow` for a literal exponent, `PowNativeDec` at run time; `A ** 2` equals `A * A`). A BLANK WHEN ZERO
floating-point edited sender holding spaces de-edits to zero instead of aborting with EC-DATA-INCOMPATIBLE, and
`DeEditFloat` validates by the `FormatFloatCore` round trip (merged here with H's rounding argument: the round trip
passes Truncation). A subscript or reference-modification expression whose digit bound passes a long takes the exact
expression lane (`Position.ExceedsLongArithmetic`). Goldens pb2617, pb2643, pb2616 (2023).

**P — PB1636 (half).** The owner signed the 32 drafted §8 derivations as one batch on 2026-10-08 ("Sign all 32").
They are rows of docs/CONFORMANCE.md §8 signed `owner: 2026-10-08`, and the batch closed all 32 (**GAP 136 → 104**).
The derivation signature is now a set (`inventory-schema.json` `derivation.signatures`, one dated owner decision per
key, read by both the Python and C# checkers), so a reading is never dated before its argument existed. R43's
definitional arm now also covers user-program constraints with no consequence and term definitions no rule reads. The
merge with train 1042 needed one fix: PB2660 landed there and RELEASED GR-12.4.5.9.4-2 (no longer contradicted on
Windows or Linux), so PB1636 is no longer blocked; it now claims that one row, which waits on the owner's signature
or a two-process test.

**R — PB2700 (landed).** A SORT or MERGE whose non-OPTIONAL USING file was missing, with no FILE STATUS clause and no
USE procedure, skipped its output procedure and carried on silently, because the implicit transfers' hook suppressed
the DOC-A.1-103 uncovered-fatal question. Each as-if statement is performed "as if" it "had been executed" (§14.9.40.4
GR12/GR15, §14.9.24.4 GR7/GR12), so `EmitUseHook` now RETURNS the question to `SortEmitter.EmitDisposition`, which asks
it through the one `EmitUncoveredFatalTermination` for all six as-if statements of both verbs, after the GIVING
write-boundary test. DOC-A.1-103's carve-out is removed. GnuCOBOL 3.2 continues here; the owner may overturn (recorded
in the row). Witnesses: four `DocA1Item103WitnessTests` and golden `85/pb2700_sort_merge_missing_using_covered`; batch
`w1041r/PB2700-batch.json` closes DOC-A.1-103, GR-14.9.40.4-12 and GR-14.9.24.4-7 (all already CONFORMS; GAP
unchanged). The re-probe found the note's MERGE repro (mg10) was a COVERED case, so the uncovered MERGE is witnessed
by a new test instead.

**Q — PB2605 (landed).** The DEVLOG rule now lives where a commit lands (owner, 2026-10-08: "Only commits landing on
main"). The commit-time hook `devlog_staged.py` read the main checkout's index, so it never saw a worktree commit. It is
now `scripts/orchestrator/landing_devlog.py`, which `push-main.sh` runs on the landing's own range before any CI run: a
landing must add a new, stamped, top-placed `## Entry` or it is refused with exit 6. The PreToolUse registration is
gone and the docs say "one entry per landing on main". This entry is the first one the check reads.

**U — PB2230, PB2233, PB2234 (landed); PB2231, PB2232 (retired).** Wave 1041's group A, preserved when train 1041b's
landing check stopped it. Deleted three census-measured dead artifacts: `db101a.txt` (NIST run output), the applied
PB59 family-5a plan, and the `push-main.ps1` forwarder. Three deletions were refused on evidence:
`prepend-devlog.py` is used (it inserted this entry) and every brief that adds a DEVLOG entry now names it, and the
local-model spike record and the consolidation-1 candidates are frozen evidence, so `census.py` no longer judges
`docs/rearchitecture/evidence/`. The sweep fixed `census.py` comparing a backslashed `str(Path)` prefix on Windows,
which counted its own records as live readers.

**V — PB2193 (landed).** Wave 1041's group G: `DataReferenceCst.Context`, `.HasNoSuffix` and `.Span` deleted (no
reader), and `CapacityRegisterReferenceDriftTests` now also refuses a raw `dataReferenceSuffix(` in
`CapacityRegisterFor`. `DataDescriptionCst.Span` is now the last caller of `SourceSpan.Of`, so PB2192 takes
`SourceSpan.cs` with it.

**N, T — notes.** N files PB2688 (the train-1040 carried-branch hand-off has no mechanism), PB2699 (the planner reads a
stale main checkout) and PB2701 (the landing-check cycle between train 1042 and the dropped D branch), records the
wave-1041 drops on PB2169, PB2193 and PB2230–PB2234, and carries `note-trend1040` (PB2426): train 1040's ledger trend
points and battery #89's Unit figure in the shape gen_ledger reads. T records the abandonment of PB2169's first
attempt in its note; it stays open for a re-plan.

**PB2703 — the landing check attributes R3 work per commit (landed).** The train's first push-main exited 3 at the
landing check with 7 stops, and none of them was a fault of any cluster. `landing_check.py` `check()` decided R3-ness
once per LANDING, but this train is mixed: U, V and T are Delete-program (PB2119) notes and the other clusters are fix
lane or tooling. That had three consequences. First, step 1 compared the whole train's diff with the R3 notes' declared
set, so 7 of its 8 "outside" files were PB2702's, Q's, P's, E's and R's. Second, the declared set came from the ledger
minus its terminal entries, and the register is read from the landing's own tree, where U flips PB2230–PB2234 to
landed. U's own 19-file entry was dropped, and with it the 8th file, `push-main.ps1`, which U deletes. Third, the
fix-lane exemption in step 2 was also per landing, so the train's fix-lane files stopped it wherever they met older
fix-lane work: train 1042's worktrees 908-1, -2, -3 and -6 on `RuntimeApi.cs`, and ledger entries w1039B and w1039F on
`DataBinder.cs` and `PicInfo.cs`. R3 work is now the commits that work on an R3 note (a leading id, or an R3 status the
commit flips) and the files those commits change. Step 1 checks only those files, against the FULL ledger's entries for
those notes, with the computed-now fallback applied per unrecorded note. Step 2 exempts a shared file unless it is R3
on one side. A squashed commit naming both kinds stays R3 whole and stops. The self-test grew from 20 to 25 cases: the
old `check()` fails four of the new plants, and the new one passes 25/25. The sweep is recorded in PB2703. `my_at` (the
landing's dispatch time) is still taken over the whole landing; the note gives its site and fix shape, and the fix
contract kept it unchanged. Two more PB2701 instances were found and appended to that note: the dispatch guard records a
lander dispatch as one 400-file `hand` entry, and six wide `Workflow:` hand entries recorded at 12:11–12:17 PDT bind
908-1 and 908-6 as R3 work. Those six entries cause the 2 stops left after the fix; run on a copy of the coordination
state without them, the check prints 0 stops.

**The train.** Ten clusters on purpose (owner 2026-10-08: fold every ready branch into the open train). The first
lander brought in E, H and G; its gate run 1 was RED on `AnnexA1RegisterDriftTests` (H's CONFORMANCE.md prose named a
row key), fixed in H. It filed PB2694 (a float-edited receiver among several arithmetic receivers still gets the
scale-hint quotient), PB2695 (the EC-BOUND-SUBSCRIPT detail prints a saturated long), PB2696 (the impact map
under-selects float-edited goldens) and PB2697 (SET index UP BY a trailing-P amount drops the P scaling, a wrong
answer), then split on STOP-loop. This lander folded in P, R, Q, U, V, N and T, took the landing lease after 6.2
minutes behind train 1042, and rebased onto it. The only textual conflicts were whole appended note paragraphs
(registrar-brief, PB2169, PB2193, PB2230–PB2234, PB2639), each resolved by keeping both sides. Train review: one
duplication finding from the merge, fixed in H. H's new `CobolNum.Magnitude` duplicated `CobolDec.UAbs`, which
train 1042's PB2641 had named the ONE magnitude, and `FloatResultant` kept a private third copy. All three are now
`CobolDec.UAbs`. Lead **PB2698** holds the intrinsic MinValue sites train 1042's F paragraph had written into PB2639,
which H's landing would otherwise orphan: FUNCTION ABS returns -2^127 for -2^127. Gate run 2 ran every case green and
was RED on one self-test: `test_dispatch_guard.py`'s "attended session" case inherited the land unit's
`COBOL_LOOP_UNIT=land`. No cluster owns it; it is fixed in the train and recorded as **PB2702**. Gate run 3: `GREEN —
Conformance 11,268/11,268 · Unit 32,928/32,928 · Characterization 36/36`. Linux gate GREEN. Semgrep: no count rose.
Oracle DIFFERENT, 91 of 7,641 vs 8d3bf88e2926, every case by class: R 51 (each SORT/MERGE whose USING/GIVING file has
no FILE STATUS gains a `TerminateOnUncoveredFatalStatus` line per as-if statement: the ST NIST programs, the sort and
merge corpus, merge-in-sort-merge-proc), H 12 (float-edited stores call `FormatFloatStore(..., mode)`; one also
shows G's de-edit), E 6 (STANDARD-COMPARE's omitted level renders `null`), G 6 (integer powers of scaled bases take
`PowNativeDec`/`CobolWide.Pow`, NC252A among them), and 16 added goldens. Re-recorded as 7ca7cc22f046. GAP 136 → 104.
No diagnostic codes claimed (E's 3206–3208, G's 3212–3214 and H's 3215–3217 were unused). The first push-main exited 3
at the landing check (7 stops, main untouched, lease released). A second lander, resuming in the same worktree, took the
lease again, put PB2703 in the train ahead of this commit, and re-gated: see the PB2703 paragraph.

