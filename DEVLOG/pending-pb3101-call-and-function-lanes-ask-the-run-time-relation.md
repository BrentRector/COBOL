## PB3127 + PB3101 (BY REFERENCE, BY CONTENT and RETURNING): every activation lane asks the run-time relation at bind time

**What was wrong.** A Format-2 CALL (a program prototype or the NESTED phrase) and a user-defined function checked
their BY REFERENCE arguments and RETURNING items with `OoConformance.DescriptionMismatch`, a comparator over `DataItem`
fields, while a CALL located at run time and both INVOKE lanes asked `ActivationRelations` over the
`ActivationDescription`s `ActivationDescriptions` builds. The two copies disagreed where ISO/IEC 1989:2023 does not.
Re-probed on `a6744fa4d`: `FUNCTION F5(Y(1:5))` into a `PIC X(5)` formal ran (`F5=ABCDE`), and so did `CALL P5 USING
Y(1:5)` (Format 2) and `CALL "S" AS NESTED USING Y(2:3)` into a `PIC X ANY LENGTH` formal, though the slice §8.4.3.3.4
GR5 creates has no PICTURE clause and §14.8.2.3.2 rule 2 requires the same one (docs/CONFORMANCE.md D-RM1, decision
PB3098). The run-time relation had its own fault (PB3127): its ANY LENGTH arm compared only category and usage, so a
JUSTIFIED, SIGN or BLANK WHEN ZERO difference passed. The note's first measured row (`CALL "P5"`, a literal) is a
Format 1 CALL, which §14.9.4.3 SR25 does not reach; the note now says so.

**The fix.** `ParameterConformance.CheckArgument` asks `CallArgumentViolation(Reference, …, specifiedProgram: true)`
for a BY REFERENCE argument (every activation it serves is one rule 2 lists), and `ReturningConformanceReason` asks
`CallReturningViolation` for every pair but two object references. The relation's ANY LENGTH arms read the any-length
form (`SameClausesAtFormalLength`): only the length is relaxed. §14.8.3.3's "Additionally" 4) and 5) now apply to an
elementary pair only; opposite an alphanumeric group an ANY LENGTH sending item is as long as its receiver (§13.18.2.4
GR1 b)). `DescriptionMismatch` is now pair mode only: `byRefGroupPrefix`, `anyLengthActivationRelax` and `invokedWith`
are deleted with their arms. Found on the way and fixed: `ActivationDescriptions.Of` described a level-66 THROUGH
alias as an elementary item, though §13.18.45.4 GR2 makes it an alphanumeric group item (this reached the universal
INVOKE lane too). `ReferenceGroupViolation` never applied §14.8.2.2 rule 1 to a fixed group opposite a
variable-length one; it now measures both in the pair lengths §8.5.1.12.3 gives. A strongly-typed group's description
rendered as `type:tc`.

**Goldens.** `conformance:2002/pb3101_activation_lanes_one_relation` (a slice into a prototyped program's and a
function's group formal, an ANY LENGTH NESTED formal, and a THROUGH alias into a group formal; every value derived from
the rule) and the negatives `pb3101-prototype-call-refmod-by-reference`, `pb3101-function-refmod-by-reference`,
`pb3101-nested-refmod-into-any-length` and `pb3127-nested-any-length-clause-differs`.
`conformance:2023/pb173_bit_group_boundary` passed a slice BY REFERENCE through the NESTED phrase into an elementary
formal, which D-RM1 refuses; its formal is now the 3-byte group D-RM1 names, with unchanged output. The
`l1c34-strong-type-currency-string-differs` expected text follows the relation's wording. A sweep over 478 positive
goldens that use a function, a prototype, NESTED or a method found no new conformance refusal; 164 conformance
negatives still reject, and 191 run-time goldens (ANY LENGTH, OCCURS DYNAMIC, universal INVOKE, RENAMES THROUGH)
produce their outputs.

**Rows.** None were open: all 22 rows under §14.8.2 and §14.8.3 are CONFORMS. PB3127 is landed; PB3101 stays open
(SPLIT), with the BY CONTENT arm, the GR8 reduction, pair mode on `SameSignature`, the deletions and
`OneRelationDriftTests` named in its note and in docs/COBOLNET_OO_DESIGN.md D6 item 7.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261014T195501Z-c8027a`; Conformance 6,097/11,669, Unit 32,911/33,618, Characterization 6/36 in leg 1; every added golden and negative passed in leg 1). LINUX GATE: the lander's (batched gating).

**Wave 1078 (the BY CONTENT / BY VALUE arm).** Wave 1077's four commits were cherry-picked onto `c29af1704` and re-gated
GREEN first (run `20261014T214044Z-9d057d`). The bind-time BY CONTENT copy was the `OoConformance` comparator family
(`ContentMismatch`, `ContentValueMismatch`, two literal arms). Re-probed on this build, it disagreed with the run-time
relation in both directions. It refused a group argument BY CONTENT into a `PIC 9(4)` formal, which §14.8.2.2 rule 2's
MOVE admits. The run-time relation let a strongly-typed group into a `PIC X` formal, because §14.8.2.2's strongly-typed
sentence was asked only BY REFERENCE. Neither copy asked §14.9.25.3 SR6 / SR7 of a figurative argument, so ZERO ran into
a `PIC A(4)` formal and SPACE into a `PIC 1(4)` one. `ParameterConformance.ContentConformanceReason` now describes the
argument (`ActivationDescriptions.OfCallArgument` / `OfValue`) and asks `ActivationRelations.CallArgumentViolation`. CALL
Format 2, functions and every INVOKE BY CONTENT shape ask it, and the comparator family and its `ContentValue` input are
deleted. Two object references stay with the class table (`OoConformance.ObjectReferenceContentMismatch`). The
compile-time SET reads their class facts as a pair, and an ACTIVE-CLASS formal's alternatives depend on the invocation.
This is a design correction, recorded in OO D6 item 7. The relation gained `StrongTypeViolation` (both modes) and SR6 /
SR7 (`MoveValidity.FigurativeRefusal`, which `MoveTable16` now asks too, so it is one copy).
`MayMove` became `MoveViolation`, which names the rule a MOVE breaks. New tests: the golden
`conformance:2002/pb3101_content_one_relation`, the negatives `pb3101-content-zero-into-alphabetic` (SR6) and
`pb3101-content-space-into-boolean` (SR7), and the drift test
`ActivationConformanceDriftTests.TheContentArgumentRule_IsTheRunTimeRelation_AskedThroughOneEntry`. Six inventory rows
had their witnesses re-sited through two record_verdicts batches; GAP is unchanged at 101. A sweep found 479 positives,
172 negatives and 442 run-time goldens clean. Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`
(run `20261014T222439Z-6cb31b`; Conformance 6,173/11,679, Unit 33,056/33,636, Characterization 6/36 in leg 1; every added
golden, negative and drift test passed). LINUX GATE: the lander's (batched gating). PB3101 stays open. Still to do: GR8
once (PB613), pair mode on `SameSignature`, the `DescriptionMismatch` deletions and `OneRelationDriftTests`.
