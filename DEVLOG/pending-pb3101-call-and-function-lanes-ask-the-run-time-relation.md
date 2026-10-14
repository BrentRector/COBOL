## PB3127 + PB3101 (BY REFERENCE and RETURNING halves): the CALL and function lanes ask the run-time relation at bind time

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
