## PB370 closes: a MOVE identifies its sender before its receiver (§14.9.49.4 GR11)

kb/Work PB370's last open row, `GR-14.9.49.4-11` ("the exception condition that is used to determine which USE
statement to select is the first one that occurs in the order of evaluation of the statement"), was held open by a
defect wave 1091e measured: `MOVE X (1:L) TO T (I)` with L and I both out of range selected EC-BOUND-SUBSCRIPT, the
RECEIVER's condition. Re-probed on the w1102f build it still did, and the defect was wider than the one leg measured:
the main build failed seven of the nine MOVE legs of the new golden, every one receiver first. Two mechanisms put the
receiver ahead. The store C# renders, `CobolTable.At(T, Occ(I)) = <sender>`, identifies its ref-returning left operand
before it evaluates its right; and a receiver's function-bearing subscript or user-function activation is a pre-op
that runs before the statement. The one leg that passed (`MOVE T (I) TO X (1:L)`) passed by accident, because a
receiving slice's bounds are checked inside `CobolString.SpliceInto`, after its arguments.

The order is §14.9.25.4 GR1's: identifier-1's reference modifier, subscript or function-identifier is "evaluated only
once, immediately before data is moved to the first of the receiving operands", and "Item identification for
identifier-2 is performed immediately before the data is moved to the respective data item"; §14.6.4's
left-to-right order agrees. GR1 also writes the shape out as an equivalence (`MOVE a (b) TO temp`), and the binder
skipped that intermediate at one receiving operand on the claim that it is unobservable there. The order of the two
identifications makes it observable. `MoveBinder.SenderIdentificationIsOverrun` now freezes the sender at N = 1 when
its own evaluation can be overtaken (the new abstract `Place.IdentifiesAtRunTime`, which every place kind must
answer, or a function-identifier) and the receiver can act first: it registered pre-ops, or it identifies at run time
while the compilation group can enable a condition at all (`TurnState.AnyEnabled`), so a program with no enabling
>>TURN emits exactly what it did. `SendingValueTemp.Positioned` is now the one placement of a freeze on the pending
list: the MOVE's freeze lands where the sender finished binding, before the receivers' pre-ops (so N > 1 is ordered
too), and `ArgumentOrder.StoreAt` uses it instead of its own copy of the relocation.

Goldens: `conformance:2002/pb370_move_first_condition` (nine MOVE legs in both orders over EC-BOUND-REF-MOD,
EC-BOUND-SUBSCRIPT, EC-BOUND-ODO, EC-ARGUMENT-FUNCTION and a BASED operand's EC-DATA-PTR-NULL, a control leg, and the nonfatal-first EVALUATE leg, where
§14.7.8 2)'s EC-RANGE-INVALID runs its declarative before the second WHEN's EC-BOUND-SUBSCRIPT), and its reject half
`negative/pb370-move-first-condition-below-2002` (COBOLNET0900 at 85). Every expected line was derived from the rules
before the first run. Batch `w1102f-pb370-gr11`: GAP 65 → 64; PB370 is landed with all seven of its rows closed. The
design doc (`DESIGN-binder-bound-tree.md`, the SendingValueTemp section) records the N = 1 order rule.

The self-review found a second mechanism the first one exposed: freezing a NUMERIC function-identifier at one
receiver made `MOVE FUNCTION PI TO S (I)` into `PIC 9V9(12)` store 3.141592653000 under >>TURN, because the §15.4
temporary had the fixed 30-digit, scale-9 description while the un-frozen MOVE renders the function at the
receiver's scale. That temporary was already wrong at N > 1 on main: `MOVE FUNCTION PI TO S1 S2` stored
3.141592653000 in both, and a 25-digit NUMVAL value lost its four high-order digits. `SendingValueTemp.ReturnedValuePicFor`
now sizes it to the receivers' widest scale and integer digits (`PicInfo.WidestReceiving`, the one fold
`ReceiverContext.OfSet` now uses too), within the 38-digit carrier; golden `2002/pb370_move_function_value_digits`
compares every multi-receiver result with its single-receiver twin, and DOC-A.1-92 records the widening.

The sibling sweep found the same receiver-first order in the arithmetic statements (`ADD FUNCTION SQRT (NEG) TO N (I)`
selects EC-BOUND-SUBSCRIPT; `ADD N (I) 1 GIVING O (1)` selects EC-BOUND-ODO), which is ArithmeticEmitter's store and a
different mechanism; it is reported as a lead.
