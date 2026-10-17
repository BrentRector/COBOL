## PB3261: a store evaluates its sending operand before it identifies the receiver

kb/Work PB3261 (wave 1102 G). `MOVE X(1:L) TO T(I)` with L and I both out of range ran the EC-BOUND-SUBSCRIPT
declarative, although ISO §14.9.25.4 GR1 evaluates identifier-1's reference modifier "immediately before data is moved
to the first of the receiving operands" and identifies identifier-2 only "immediately before the data is moved", so
§14.9.49.4 GR11 ("the first one that occurs in the order of evaluation") selects EC-BOUND-REF-MOD. The cause was not
MOVE's: every store renders as `<receiver> = <value>`, and C# evaluates the left operand's address first, so
`CobolTable.At(TB.T, CobolTable.Occ(I, ...)) = CobolString.Store(CobolString.RefMod(X, ...), 4)` range-tested the
subscript before the reference modifier. A reference-modified receiver's splice inverts the same way through its
argument list (start and length before the value).

**Re-probe.** Reproduced on this build with the PB370 probe. The sibling sweep found three more arms on the root:
`MOVE X(1:L) TO N(I)` (a numeric receiver), `STRING X(1:L) DELIMITED SIZE INTO T(I)` (the emitter read identifier-3
into its working image before any sender, so the hoisted identification froze positions but the range tests ran in
the wrong order) and `ADD FUNCTION NUMVAL(X(1:L)) TO N(I)` (the in-place combine read N(I), the operand written last,
first). COMPUTE and GIVING were right only under EC-SIZE checking, whose size-error path happens to stage the value.

**Fix.** One store primitive, `PlaceIdentification.SenderFirst`: when the sender and the receiver both identify at run
time it evaluates the value into a block-scoped `__sending` local before the store; otherwise the store is rendered as
before, so a static receiver's generated code does not change. `PlaceIdentification.IdentifiesAtRunTime` answers the
question for a place by asking `Freeze` whether it would hoist a fragment (plus an occurs-depending length and an
omitted-formal guard), so the two cannot drift. Every MoveEmitter store arm renders through it; INITIALIZE keeps the
receiver first (`ImplicitMovePhrase.ReceiverIdentifiedFirst`: §14.6.4 7)'s left-to-right order identifies
identifier-1 before the REPLACING sender, a determination recorded on the note), and so does an overlap prefill. The
pointer-family SET stores take it too (§14.9.39.4 GR12, GR14, GR16). The arithmetic statements now materialize the
initial evaluation for one receiver that identifies at run time, as they already did for several
(`ArithmeticEmitter.MaterializesInitialEvaluation`, §14.7.7 rule 4). STRING reads each sending operand and delimiter
that identifies at run time, in statement order, before identifier-3 and the POINTER value (§14.6.4 7)). The
MoveBinder comment that called the single-receiver intermediate "unobservable" now says what is and is not: its value
is, its order is not. Found on the way: SET program-pointer cited §14.9.39.4 GR18 (Format 10) for Format 9's GR16.

**Golden.** `conformance:2002/pb3261_sender_first_order`: thirteen statements that fail on both sides (five MOVE
shapes in both directions, four arithmetic forms, two STRING positions, INITIALIZE), the nonfatal-first EVALUATE shape,
and in-range legs that check the stored values. No negative: the order is not edition-gated.

**Rows.** GR-14.9.49.4-11 PARTIAL → CONFORMS (batch `w1102g-pb3261`, GAP 60 → 59); PB3261 and PB370 landed.

**Self-review.** Architecture: one primitive in the item-identification class, its predicate derived from Freeze;
the verb emitters only decide whether their rule is sender-first. Full code: each arm cites the rule it implements;
INITIALIZE's receiver-first order is the one deliberate exception. Performance: a hoist is emitted only when both
sides evaluate at run time, a JIT-elided local. Duplication: no second copy of the order rule; the arithmetic snapshot
reuses the existing `Snapshot`.

**Gate.** The first gate was red on `MoveOverlapDriftTests` (it pins exactly two reads of `m.Stores[i].Prefill`; the
order decision read a third), so the overlap-prefill exception moved into `EmitStore` beside the prefill it reads. The
re-gate (run `20261017T031527Z-6d6fa0`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` —
Conformance 5,888 · Unit 33,173 · Characterization 5 cases ran in leg 1, the golden among them.
