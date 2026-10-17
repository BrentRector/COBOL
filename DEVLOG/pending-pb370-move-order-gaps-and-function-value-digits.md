## PB370: a MOVE orders its sender before a receiver's pre-op and an occurs-depending receiver, and its frozen function value keeps every digit

This branch began in wave 1102 F as the fix for `GR-14.9.49.4-11` (§14.9.49.4 GR11: "the first one that occurs in
the order of evaluation of the statement"): `MOVE X (1:L) TO T (I)` with L and I both out of range selected the
receiver's EC-BOUND-SUBSCRIPT. Before it landed, kb/Work PB3261 landed the store half of the same order on main
(`PlaceIdentification.SenderFirst`, which evaluates the sending value into a local ahead of the receiver's address).
Wave 1107 A merged the two. It dropped this branch's own N = 1 order freeze (`MoveBinder.SenderIdentificationIsOverrun`)
and its abstract `Place.IdentifiesAtRunTime`, a second copy of `PlaceIdentification.IdentifiesAtRunTime`, and kept
what main still lacked.

**Re-probe on the merged build.** This branch's golden `2002/pb370_move_first_condition` failed three of its legs on
PB3261's mechanism:
- C `MOVE N (I) TO O (1)` and D `MOVE O (1) TO N (I)`, where O is in an occurs-depending table. The literal
  subscript's bound test (§13.18.38.4 GR7, "At the time the subject of entry is referenced … the value of the data item
  referenced by data-name-1 shall fall within the bounds") rides the table step, `FixedTableSegment.Odo`. But
  `IdentifiesAtRunTime` asked only `Freeze`, which leaves a bare literal alone, so it answered "nothing" and the store
  stayed receiver first. It now counts that test and an OCCURS DYNAMIC element (`PathEvaluates`, `DynTablePlace`).
- F `MOVE X (1:L) TO T (FUNCTION SQRT (NEG))`. The receiver's function-bearing subscript is a pre-op, so it runs before
  the statement whatever order the store has. `MoveBinder.BindMoveOf` now freezes the sender at N = 1 when the receiver
  registered a pre-op and the sender identifies at run time. It places the freeze where the sender finished binding
  (`SendingValueTemp.Positioned`, the one placement, which `ArgumentOrder.StoreAt` now uses too), ahead of the
  receiver's pre-ops. The same placement orders N > 1.

**The frozen function value's digits (mechanism 2, with review finding R3-1).** §14.9.25.4 GR1's equivalence
(`MOVE a TO temp` / `MOVE temp TO b`) holds only if the intermediate keeps every digit a receiver takes. The fixed
30-digit, scale-9 temporary stored `MOVE FUNCTION PI TO S1 S2` into `PIC 9V9(12)` as 3.141592653000, where
`MOVE FUNCTION PI TO S1` stores 3.141592653589. `SendingValueTemp.ReturnedValuePicFor` sizes the temporary from
`ReceivingReach.Of`:
- a floating-point item for a floating-point set;
- `+9.9(35)E+9999` for a floating-point set that holds an edited receiver;
- exactly the receivers' widest digits for an all-fixed-point set.

The branch review (R3-1) found the last shape inexact when those digits pass the 38-digit carrier. With `PIC 9(31)`
beside `PIC V9(28)`, the capped scale cut `MOVE FUNCTION NUMVAL ("1234567890123456789012.123456789") TO X Y`'s `Y` to
1234567000…, where its single-receiver twin holds 1234567890…. Main had cut `X` instead. A non-integer value in such a
set now takes the 36-digit floating-point edited intermediate (`ReceivingReach.BeyondCarrier`); an INTEGER function
keeps scale 0, which fits. Measured on the merged build, NUMVAL's X and Y both equal their twins. A binary64 value
(PI, SQRT, EXP) reaches the receivers through its shortest round-trip decimal, where its single-receiver MOVE lands the
binary expansion. The two agree in every digit binary64 carries, the old window lost seven to twenty-one fraction
digits, and DOC-A.1-92 records the limit. `ReceivingReach.CarrierDigits` is `ReceiverContext.IntermediateDigits`, so
SendingValueTemp's private copy of 38 is gone.

**Goldens.**
- `2002/pb370_move_function_value_digits` gains leg B: unsigned and signed NUMVAL into `9(31)` with `V9(28)`, and an
  INTEGER function into the same set. On the predecessor head the NUMVAL legs print DIFFER.
- That golden's single-receiver subscripted legs (O L M N Q Y Z V J) became two-receiver legs. A single subscripted
  receiver no longer freezes, so they would not have exercised the intermediate any more.
- `2002/pb370_move_first_condition` and its reject half, `negative/pb370-move-first-condition-below-2002`, ship as they
  were written in wave 1102 F.

**Branch review round 1 (finding R1-1): an object-property receiver.** The leg-F freeze watched only the statement's
pending pre-op list. An object-property receiver moves its identifier-3's pre-ops off that list into its accessor
bracket (`OoPendingPropertyOp.Prelude`), and `ReceiverBracketEmitter.Receive` opens the bracket just before the store,
together with identifier-3's evaluation into the identified-object temporary and the GET of a part store. Three legs
measured wrong on the reviewed head: `MOVE X (1:L) TO P OF R (FUNCTION SQRT (NEG))` selected EC-ARGUMENT-FUNCTION,
`MOVE X (1:L) TO P OF R (I)` selected EC-BOUND-SUBSCRIPT, and `MOVE X (1:L) TO Q OF R (1) (1:2)` ran Q's get method
before the sender's EC-BOUND-REF-MOD. `DataBinder.OoReceiverOpensAtRunTime` now answers whether a claimed property's
bracket evaluates anything at run time. It reads the members the bracket is built from, which `OoBinder.OoWrapPropertyOps`
now reads too: `OoPendingPropertyOp.IdentifiedReceiver` and `GetsBefore`. The part-store polarity is
`BoundStores.WholeReceiverStore`, which the store walk also asks. `MoveBinder.BindMoveOf` freezes the sender on that
answer as it does on a pre-op. The new golden `2002/pb370_move_property_receiver_order` has legs K, L and N, the
sending-property mirror M, and an in-range control. On the reviewed head K, L and N print the receiver's condition.

**Rows.** No row changes owner.
- `GR-14.9.49.4-11` stays PB3261's and gains `pb370_move_first_condition` as a witness (batch `w1107a-pb370-gr11`).
- `GR-14.9.25.4-1` is re-verdicted CONFORMS with current notes (batch `w1107a-pb370-gr1`). GAP 58 → 58.

**Not fixed here (leads).** The same pre-op gap reaches the arithmetic statements:
`COMPUTE N (FUNCTION SQRT (NEG)) = N (I)` selects EC-ARGUMENT-FUNCTION, where §14.7.7 4) b) identifies the receivers
after the initial evaluation. A property receiver there has the bracket form of the same gap: `COMPUTE NP OF R (I) =
FUNCTION SQRT (NEG)` selects EC-BOUND-SUBSCRIPT. STRING also identifies a BASED identifier-3 at its statement-head freeze:
`STRING T (I) DELIMITED BY SIZE INTO BX`, with BX's address NULL, selects EC-DATA-PTR-NULL.

**Self-review.** One mechanism per job holds: the store order stays `PlaceIdentification.SenderFirst`'s, the pre-op
order is the binder's one freeze, and the run-time question has one answer, which the binder asks as well (the binder
already calls the code generator's `FigurativeConstants` the same way). Every leg is compared against its un-frozen
single-receiver twin. The build has 0 warnings and 0 errors, and `semgrep/verify.py` PASS shows no rule's count rose.

**Gate.** Run `20261017T124929Z-c76990`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2
left to CI's whole-population run: Conformance 5,088 · Unit 377 · Characterization 31 … ===`. All three PB370 corpus
cases and `pb3261_sender_first_order` passed in leg 1, and the leg-1 trx files hold 0 failed results. After the R1-1
fix, run `20261017T131742Z-9c0235`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to
CI's whole-population run: Conformance 4,611 · Unit 371 · Characterization 31 … ===`. The new golden
`pb370_move_property_receiver_order`, the other PB370 cases, the `ReceiverBracketDriftTests` and the property-receiver
goldens (`pb2618_*`, `pb2078_property_receivers_in_phrases`, `oo_property_ref`) passed in leg 1, with 0 failed results.
`semgrep/verify.py` PASS.
