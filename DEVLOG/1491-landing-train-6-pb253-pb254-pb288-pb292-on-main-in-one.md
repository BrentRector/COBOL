## Entry 1491 — 2026-09-05 05:20 PDT — Landing train 6: PB253 + PB254 + PB288 + PB292 on main in ONE landing (GAP 2864 → 2850)

Four clusters, four cluster commits plus a reconciliation, the inventory and the prose. No cluster dropped. The
one attributed red was a cross-cluster reconciliation with train 5's PB248, not a defect in any cluster, and it
is fixed inside the train rather than deferred.

**PB253 — the arithmetic mode decides the container, and it is asked first.** `IntrinsicRenderer.RenderFloat`
tested the RECEIVER SHAPE before the arithmetic mode, so under `ARITHMETIC IS STANDARD-DECIMAL` — and under
2002's plain `ARITHMETIC IS STANDARD`, which `ArithmeticModes.IsDecimalEngine` also selects — the SDIDI arm
below it was unreachable for every receiver-less or float-receiver reference and a raw binary64 escaped. §15.4.1
is unconditional: "the returned value for numeric and integer functions is contained in a temporary standard
data item in the intermediate form defined for the arithmetic mode in effect", and §8.8.1.5.1 makes the mode "a
method of evaluating an arithmetic expression, an arithmetic statement, the SUM clause, **and certain integer
and numeric functions as specified in 15.4.1**" — the function itself, not only its use as an operand. §15.4.1's
last paragraph exempts the VALUE, never the CONTAINER, and the arm that ran in every mode was licensed by
§15.4.1's NATIVE-mode sentence — a real clause answering a different question. The harm was not a rendering
difference: `MOVE FUNCTION TAN(a)` landed 16331239353195368.96 while `COMPUTE r = FUNCTION TAN(a)` landed
16331239353195370.00 for the SAME call, and `FUNCTION RANDOM(7)` split 0.3832204692918902784 /
0.38322046929189024 — two returned values for one function and one argument in a single run, which §15.4.1
forbids by name; `DISPLAY FUNCTION SIN(1E-20)` printed `1E-20` where the SDIDI's item-92 text is
`0.00000000000000000001`. The fix is the ORDER, made STRUCTURAL rather than positional: one method became three,
`RenderFloat` now reads the mode and nothing else, and the D16 receiver-shape arms moved to a mode-blind
`RenderFloatNative` that cannot pre-empt the container rule — `num.StandardDecimal` is read at exactly one place
in this family. It moves the whole residue at once: ACOS, ASIN, ATAN, COS, SIN, TAN, LOG, LOG10, RANDOM, every
`Float: true` catalog row the standard-mode arm had not already claimed. The sweep asked "which arm did you fix"
of every sibling receiver-shape test in the numeric renderers — `CombineCore`, `Power`, `Landed` and
`ConditionRenderer` were already mode-first; `RenderFloat` was the one site not keeping the ordering
`COBOLNET_NUMERIC_DESIGN.md` D3 already states for the whole engine, and that doc's prose-family paragraph was
wrong in the same way the code was ("converting into expressions per §8.8.1.5.2 r1" — the OPERAND case only),
so it is corrected in the same change set. Three goldens at all three editions the rule reaches
(`pb253_prose_float_standard_container` 2002, `pb253_prose_float_sdidi_container` 2014 + 2023), each proved to
FAIL against the pre-fix compiler, plus `StandardModeReturnedValueContainerDriftTests`, whose fixture table is
checked against the catalog source both ways and whose failure branch fired once and named exactly the nine.
`RV-15.89.4-1` PARTIAL → CONFORMS. No diagnostic code claimed.

**PB254 — a TOTAL function was inside a PARTIAL function's landing, and the wrong-answer half was in the lane
the note called a footnote.** `TEST-DATE-YYYYMMDD` and `TEST-DAY-YYYYDDD` are total over the integers: §15.90.3
r1 and §15.91.3 r1 constrain nothing but integer-ness and r1a is a catch-all, so §15.3 rule 14 — which fires
only on "an incorrect value … according to the rules specified in the function definition" — has nothing to key
on. PB22's narrowing landing sat in front of both anyway. **Re-probing found the note's mechanism wrong.** The
note said the body is never called and the compiler answers VALID; the body IS called, because
`ExceptionState.ArgumentError`'s checking-off `return 0` substitutes for the ARGUMENT, so the native and Dec
lanes answered the RIGHT code by coincidence — 0 lands in r1a's lower half. The defective lanes are the other
two: the FLOAT twin's `TryIntegerArg(…) ? body : 0` tail answered **0 = "the date is valid"** for a `COMP-2`
holding 1.0E19, eleven orders of magnitude outside the window whose violation the function exists to report; and
under `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON` a `PIC 9(19)` witness terminated the run unit fatally on source
§8.3.3.3.2 makes legal. Fixed at the validators: both bodies take `Int128` and answer r1a before any conversion,
and the `…Real` twins route through a saturating `TryTotalIntegerArg` that is verdict-preserving *because* r1a
is a catch-all, so the windows stay written in one body; a NaN — the one input that is the value of no integer —
takes the shared §15.3 raise site with the documented result 1, never the 0 that calls a non-date a valid date.
Swept the sibling the same day: FIND-STRING argument-3 (§15.37.3 r3, no value constraint) returned match
position 1, and 10 under `LAST`, where §15.37.4 r3 requires zero — kb/Work R08 had removed the same narrowing
ONE LEVEL DOWN and left the intake, and its golden stops at `PIC 9(18)`, one order of magnitude below where the
defect lived. The structural half: the intake width is now DERIVED from the runtime body's declared carrier
(`Int128` = total, `long` = bounded) and `IntrinsicCarrierAgreementDriftTests` re-derives that pairing by
reflection and fails on disagreement in either direction — proven red both ways, and it caught a real accidental
widening of the INTEGER-OF-DATE family inside the change set. `2002/pb254_total_integer_arguments` pins r1a's
upper half under ARMED checking with a deliberate control raise; `2023/find_string` gains three wide legs;
`DESIGN-runtime-library.md` §2.7 states the BOUNDED/TOTAL split and that membership is not a maintained list
anywhere. `RV-15.90.4-1` / `RV-15.91.4-1` PARTIAL → CONFORMS; `RV-15.37.4-2` / `-3` keep CONFORMS with the
evidence corrected — both were green while their widest half was unmeasured. No diagnostic code claimed.

**PB288 — the CALL argument's numeric landing is one rule now, and it is §14.2.3 GR9/GR10's COMPUTE.** A `CALL …
BY CONTENT 1000000000000000000000000000000` into a `PIC S9(9)V9(9)` formal arrived as +873995514.006732800
through the GR8 aliasing view and as −123822295.304634368 through the GR10 value copy — two different wrong
answers for the same argument, which is the tell that the two arms were never one rule. **The note's proposed
fix was wrong and the spec says so:** it asked for a CHECKED, raising widening, but GR9 and GR10 both make the
crossing to a numeric formal "a COMPUTE statement without the ROUNDED phrase" into a record of the FORMAL's
description, and GR11 resolves every reference through that same description — so an over-capacity argument is
§14.7.5 **case 3**, a RECEIVER overflow "after radix point alignment", whose no-phrase checking-off disposition
is the one `CONFORMANCE.md` DOC-A.1-70 already documents: the result's LOW-ORDER digits. Raising would have made
the CALL crossing disagree with the very COMPUTE the standard says it is, and the correct primitive already
existed — `CobolNum.RescaleStoreCap`, which DOC-A.1-70 names by name — it simply was not on this path.
`CobolArgAdapt.Land` is now the single landing every numeric arm takes, composing the two halves each of which
was missing somewhere: the store-semantics widening (the wrap the note found, at 39 digits) and the
digit-capacity conformance before the closing `T.CreateTruncating`, which is itself a binary truncation to the
carrier — **a second defect the note does not contain**, bigger, firing at 19 digits, which made a `PIC S9(4)V99`
formal show −9838.16 where the inline MOVE and COMPUTE both say +5678.00. The float lane had the same split with
the arms swapped. Two of the note's five named sites needed nothing: `Text`/`TextValue`/`StoreReturnNum` format
at the argument's own description through a decimal digit cap and cannot wrap. Sibling sweep: `Int128.Abs`
THROWS on `Int128.MinValue` and BOTH widening siblings called it, and that value is reachable through this very
ABI, so putting a per-access path on `RescaleStoreCap` would have converted a wrong answer into a CRASH — both
are now MinValue-safe, and the cap test became a power-of-ten compare instead of a 38-iteration division loop
because the GR8 view re-lands on every reference to the formal. Golden
`2023/pb288_call_argument_scale_landing` — nine rows, six red on the pre-fix runtime, three of them the inline
MOVE/COMPUTE comparators that make "the crossing IS that COMPUTE" testable — plus
`CallAbiNumericCarrierDriftTests.TheGr8ViewAndTheGr10Copy_LandIdentically` and a low-order-38-digit equivalence
sweep. `GR-14.2.3-9` / `GR-14.2.3-10` are recorded PARTIAL against the OPEN note **PB640** (the checking-ON half
that GR9 puts caller-side and this implementation performs callee-side); the batch was withdrawn on the
implementer's tree precisely because a PARTIAL row needs an open claiming note, and lands here with its `notes`
prefix rewritten. No diagnostic code claimed.

**PB292 — RECORD DELIMITER was declined in the documentation and accepted in silence; now it is declined by
name, on both arms, at every edition.** `docs/CONFORMANCE.md` §2 row 26 has said "Not claimed" since the
document was created, and §2's own preamble promised "a §4.2.6 warning is emitted where the element is
syntactically detectable" — but `recordDelimiterClause` parsed into nothing, so `RECORD DELIMITER IS STANDARD-1`
compiled and ran clean at 85/2002/2014/2023, and so did the feature-name arm. That preamble sentence was FALSE
for this row and is now true. `COBOLNET1778` `record-delimiter-unsupported` (accept-inert Warning, in the
existing band beside 1578/1579/1580) is raised from `DataBinder.BindFileControl` through the ONE
`EditionContext.Declined` seam, keyed on the WHOLE grammar rule so one arm cannot be fixed without the other.
The two arms turn out to be declined on *different* licences, which is exactly why one descriptor names both:
STANDARD-1 is Annex A.3 item 26 with §12.4.5.11.4 GR2's tape drive, while feature-name-1 is Annex A.1 item 150,
an *optional* implementor-defined element whose available-name set this implementation now specifies as EMPTY.
It is accept-inert rather than refused because §12.4.5.11.4 GR1 keeps the framing "out of the record area and
the record size used within the function, method, or program" — unlike an inert FORMAT clause, an ignored RECORD
DELIMITER changes nothing a program can see, and GR5's implementor method (the 4-byte little-endian length
prefix, now filed as A.1 item 151) governs either way. ⛔ **Row 26 has no owner decision behind it** — `git log
-S` puts it in the document's creating commit, never revisited — so the warning landed on the ground that it is
required under EITHER branch of the still-open question. The note's placeholder sweep grew from five rows to
**eight** (`8.x`, `9.x` and `11.x` are the same defect in other chapters), every one derived and `--check`ed,
and the class is now mechanically closed by the new `AnnexA3RegisterDriftTests`, which also holds the register to
covering all 46 A.3 items and to naming only codes the compiler really EMITS — an oracle chosen deliberately
over catalogue membership, because four rows cite codes still emitted as bare string literals and a
catalogue-only test would have been weakened or deleted instead. On the way, `RecordFraming`'s header was found
citing §9.1.13.6's "externally-defined boundaries" — the I-O status 34 rule about writing past a *file's* end —
for its record framing; the real licence is §9.1.7.2's "record length headers". Witness
`2023/pb292_record_delimiter_witness` (four files: both arms, the optional-word-omitted spelling, and a file
with NO clause at all, which is both GR5's antecedent and the complement proving the diagnostic can be silent)
plus five facts in `DocumentedNonSupportWitnessTests` at all four editions. Eleven rows closed. COBOLNET1778 is
claimed; COBOLNET1779 returns to the pool.

**⚠ OWNER QUESTION, raised by PB292 and deliberately not answered here.** Should COBOL.NET ever *provide* the
RECORD DELIMITER facility — either the STANDARD-1 arm's ISO/IEC 1001:2012 7.1.2 tape framing (§12.4.5.11.4 GR3)
or a feature-name of its own under Annex A.1 item 150 — rather than keeping the documented decline? The decline
has never been adjudicated; it is an authoring choice inherited from the commit that created
`docs/CONFORMANCE.md`. Two facts bear on it: §12.4.5.11.4 GR2 requires the external medium to be a tape drive
for STANDARD-1, which no COBOL.NET target has (the same determination §2 rows 28–30 and 33–34 already record);
and ISO/IEC 1001:2012 is a separate standard not present in `specs/`, so its 7.1.2 method cannot be derived from
the COBOL spec and would have to be sourced first. The §4.2.6 ¶3 warning that landed today is required under
either answer, so nothing in this landing pre-empts the decision.

**⛔ THE TRAIN'S ONE RED, AND WHY THE CLUSTER WAS RECONCILED RATHER THAN DROPPED.** The first union gate failed
exactly one case: `CorpusRunnerTests_P2(edition: "2002", name: "pb254_total_integer_arguments")`, COBOLNET1627 on
its T7 and U7 legs. PB254 was cut at `1563ccf5`, BEFORE landing train 5 put **PB248** on main, and PB248
re-derived §15.3 type 6 as "an integer data item or an always-integral arithmetic expression" — a floating-point
item is neither, because §14.6.8.3 makes its declared value set contain non-integers whatever a given reference
holds. PB254's golden knew it was leaning on the old latitude; its own header said the float carrier is one
"which §15.3 type 6 does not describe but the binder admits because integer-ness is a VALUE property (PB21)",
and that premise is precisely what PB248 retired. So those two legs were testing NON-CONFORMING source inside
the STRICT positive corpus and they came out — the same move PB248 itself made when it retired
`2023/pb21_float_arg_integer_family` rather than weaken the screen. **The cluster is not dropped and the feature
is not narrowed:** PB254's float lane is the wrong-answer half of its defect and is still live code, reachable
under `--permissive`, so its witness moved to `FloatIntegerArgumentPermissiveTests` — the home PB248 created for
exactly this, beside PB21's and PB2's programs, which moved for the same reason — asserting F1=01/F2=01 (r1a on
1.0E19) with an in-program control F3=00 on the same COMP-2 carrier, so a body answering 1 for everything would
fail there too, plus a sibling fact pinning that strict still refuses the program. The note and
`DESIGN-runtime-library.md` §2.7 both record it: the LANE changed, the obligation did not.

**The train.** Union filter of the four clusters' terms plus `IntrinsicArgumentIntakeContract` and
`ExactCarrierBoundary` (train 5's drift suites, because PB253 and PB254 both edit `IntrinsicRenderer.cs`) and,
after the reconciliation, `FloatIntegerArgument` and `GrammarDiagramGenerator` — 32 `FullyQualifiedName~` terms.
The gate is `Passed! - Failed: 0, Passed: 2539, Skipped: 0, Total: 2539` on Conformance, `Failed! - Failed: 2, Passed: 5432, Skipped: 0, Total: 5434` on Unit and `Passed! - Failed: 0, Passed: 33, Skipped: 0, Total: 33` on Characterization — the conformance leg, where all six new goldens run, fully green, and `GrammarDiagramGeneratorDriftTests.Generator_RunsClean` (PB588, load-sensitive) passed inside the filter. Every remaining red is the known fresh-worktree shape:
`ExternalCorpusPopulationDriftTests.TheCensus_NamesEveryPopulationAndItsProgramCount` and
`.SweepExternalPopulation_EqualsTheDifferentialsCommittedCaseCount` — no GPL corpus in a worktree, never `git
add`ed. `python scripts/semgrep/verify.py` PASS with every count equal to its baseline (3/3, 36/36, 2/2,
441/441): the landing increases none. Both citation audits at zero — `audit_code_citations.py` 0 findings over
3219 files, `audit_doc_citations.py` 0 MISFILED over 301 verbatim-shaped citations — and `work.py check` 691
work items, all well-formed. The five verdict batches were RE-APPLIED on the merged tree in manifest order
rather than merged as JSON hunks, and each cluster's own base→branch inventory delta was diffed row by row
against its batch's claims first (PB253 1/1, PB254 4/4, PB292 11/11 — every changed row covered, no batch row
without a delta), then `build_inventory.py` once: 16 records submitted, 16 accepted, **GAP 2864 → 2850**.

**Merge reconciliations worth recording.** PB253 was cut before train 5 landed PB251 and PB252 on the same file,
so `IntrinsicRenderer.cs` carried three independent corrections into one region: all three are kept, and the
three drift suites that hold them (`StandardModeReturnedValueContainer`, `IntrinsicArgumentIntakeContract`,
`ExactCarrierBoundary`) all ran green. PB254's `IntArgWide` carried a best-effort `INTAKE(integer)` remark
written before PB251's vocabulary existed; it landed as `INTAKE(INTEGRAL)`, the class
`IntrinsicArgumentIntakeContractDriftTests` derives from source, and `IntArg` kept PB251's INTAKE line while
gaining PB254's PARTIAL-only warning rather than one replacing the other. `docs/DIAGNOSTICS.md` was regenerated
once on the merged tree and came back byte-identical to PB292's, and `docs/CONFORMANCE.md` took PB288's
DOC-A.1-70 edit and PB292's §2/§7 rewrite without conflict.

