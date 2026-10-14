## PB3128: a slice of an ANY LENGTH or DYNAMIC LENGTH item is measured by identifier-1's bound, and the ACCEPT width reads one renderer

kb/Work PB3128 (wave 1072 group E). A reference-modified slice of an ANY LENGTH or DYNAMIC LENGTH item, passed BY
REFERENCE to a typed group formal, was refused COBOLNET0828 "the formal parameter (4 character positions) is larger than
the argument (0)". The re-probe on this worktree's build reproduced it for `LA(1:M)`, `LA(3:)` (ANY LENGTH formal of a
method) and `D(1:N)`, `D(3:)` (DYNAMIC LENGTH); the universal twin compiled, because its run time overwrites the
description's length with the evaluated one. The sibling sweep found the same root in two more places, both silent
wrong answers: `MOVE LA(2:5) TO X1 X2` carried ONE character through its §14.9.25.4 GR1 intermediate (`x1=B     `), and
an omitted-length ACCEPT into a slice transferred the wrong number of characters: none for `D(3:)` and `LA(2:)`, three
for `S(2:)` of a `PIC S9(4) SIGN LEADING SEPARATE` item (`-0009` for input `0099`).

The root was one question answered from the wrong fact. identifier-1's positions are fixed by its description only
when it is neither ANY LENGTH nor DYNAMIC LENGTH. `RefModPlace.InnerPositions` gave an ANY LENGTH item its one PICTURE
symbol, the activation description fell to zero when `StaticInnerPositions` was null, and `AcceptDisplayEmitter`
re-spelled the count as `OperandPic?.Length ?? ImageWidth`. Now two readers each answer one question.
`RefModPlace.MaximumInnerPositions` (replacing `InnerPositions`) is the bound: the static count, a DYNAMIC LENGTH
item's §8.5.1.10.1 maximum size, and for an ANY LENGTH item, whose description fixes none (§13.18.2.4 GR1 b)), the
carrier's ceiling `CobolDynString.MaxLength`. The intermediate's capacity and the activation description's maximum
extent read it. docs/CONFORMANCE.md D-RM1 a) and COBOLNET_OO_DESIGN D6 item 6 now say how such a slice's maximum
extent is measured. The OO design's stale "such a slice states no storage AREA" is corrected too: PB2077 round 3 made it
state its evaluated extent. `PlaceRenderer.SliceLength` is the one run-time evaluated-length renderer: the specified
length, or identifier-1's static count or current length from the leftmost position (§8.5.1.10.4, §13.18.2.4 GR1 b)).
The ACCEPT transfer and `CallEmitter.AreaOf`'s extent both take it, and `CallEmitter.SliceExtent` is deleted.
`ConditionBinder.StaticBoolRefLength` (exact static length; null fails open) was already right and `PtrBinder`'s
range-check size answers another question (ADDRESS OF a dynamic-length item is refused, §8.4.3.11.3 SR6).

Goldens: `conformance:2002/pb3128_refmod_any_length_argument` (typed slices at, above and below the formal's width, the
GR1 intermediate, the universal lane), `conformance:2014/pb3128_refmod_dynamic_length_argument`, the negative
`negative/pb3128-refmod-dynamic-limit-below-group-formal` (a `LIMIT 3` slice into a 4-byte group formal is still
COBOLNET0828 on its maximum), and two `AcceptDifferentialTests` facts (SIGN SEPARATE at 85; DYNAMIC LENGTH and ANY
LENGTH at 2014). No inventory row was open on the mechanism (`closes_rows: []` with its reason). No diagnostic code used.

Gate: `TestResults/build-local/20261014T005204Z-760695` — `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 5,591/11,548 · Unit 32,405/33,496 · Characterization 6/36 in leg 1; leg 2 left to CI). Its first run went RED on two source-pinning Unit tests that read the deleted ACCEPT re-spelling (`ReceivingStoreDriftTests` exempts the slice arm only as `{len}`; `BoundaryImageChannelTests` now pins `PlaceRenderer.SliceLength`). The self-review also corrected a comment that claimed a COMP item can be reference-modified (§8.4.3.3.3 SR1 admits only usage display or national numeric items).

Branch review round 1 (w1072e-review-r1) found two things, both fixed by the review finisher. R1-1: the SIGN SEPARATE sweep missed its USAGE NATIONAL twin. `RefModPlace.PositionCount` read the PICTURE length for every national item, which is the digit count alone, so `PIC S9(4) USAGE NATIONAL SIGN LEADING SEPARATE` measured four positions where §8.4.3.3.4 GR3 and GR4 give five ("the sign position is assigned an ordinal number within that data item"). Re-probed on the merged build: `ACCEPT S(2:)` gave `-0009`, and `T(1:5)`, `T(5:1)` and `S2(2:4)` were refused COBOLNET2670. Now only a bit item, and a national group through its as-if PICTURE, read `OperandPic.Length`; an elementary national item takes `ImageWidth`, which counts national positions and the separate sign. After: `-0099`, and the slices compile and run. Pinned by `conformance:2002/pb3128_refmod_national_sign_separate` and `AcceptDifferentialTests.Device_OmittedLengthNationalSignSeparateSlice_CountsTheSignPosition`; the edition gate is USAGE NATIONAL's own COBOLNET0900, already pinned. R1-2: the §8.5.1.10.1 quotation "the smallest of" is not the standard's wording ("is smallest of:"), and `cite.py --check` fails on it. The same misquote was in twelve more places (`DataBinder`, `DataItem`, `RecordClauseRules`, `CobolDynString`, `docs/CONFORMANCE.md`, two test files and three conformance sources); all now quote the clause as written or paraphrase it without quotation marks.

Review-finisher gate: `TestResults/build-local/20261014T014844Z-d295f3` (gated eedd43fdc) - `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 5,823/11,548 · Unit 32,530/33,497 · Characterization 7/36 in leg 1; leg 2 left to CI). semgrep verify: PASS, no count moved.
