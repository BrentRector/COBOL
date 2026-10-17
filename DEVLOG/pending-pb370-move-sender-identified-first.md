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

The branch review (round 1, finding R1-1) found the digits fix one shape short: a FLOATING-POINT receiver. The fixed-point
temporary cannot carry a binary64 receiver's value at all, so the new N = 1 freeze made `MOVE FUNCTION PI TO F (I)` into
a FLOAT-LONG store 3.141592653 under `>>TURN EC-BOUND CHECKING ON` (and the full 3.141592653589793 without it), and
`FUNCTION EXP (100)` 4.004359629035547E+20 where the un-frozen MOVE stores 2.6881171418161356E+43; the same truncation
already stood on main at N > 1 (`MOVE FUNCTION PI TO F1 F2`), and a floating-point numeric-edited receiver
(`PIC +9.9(9)E+99`) lost the magnitude the same way. The intermediate's shape is now decided by the receivers in ONE
place, `ReceivingReach.Of`: when every receiver is a floating-point item or a floating-point edited one the intermediate
is itself floating-point (the receivers' one usage, so `MOVE temp TO b` is §14.9.25.4 GR6 c)'s same-usage transfer
"without change"; binary32 for several binary32 usages; binary64 otherwise), and a floating-point receiver beside
fixed-point ones takes the fixed-point intermediate at every fraction digit the 38-digit carrier keeps (the documented
limit of a mixed set). `MoveBinder.ReceivingReach` is deleted; golden `2002/pb370_move_function_value_digits` gains legs
L, M, N, Q, R, X and Y (FLOAT-LONG, FLOAT-SHORT, an exact-valued NUMVAL, two floating receivers, a mixed pair and a
floating-point edited receiver), six of which fail on the reviewed head. The sweep of the other `SendingValueTemp`
callers found the same fixed scale-9 temporary under an EVALUATE subject (`EVALUATE FUNCTION PI WHEN 1 ... WHEN G`
takes WHEN OTHER where `IF FUNCTION PI = G` is true), which has no receivers to decide the shape from; it is reported as
a lead.

**Branch review round 2 (R2-1): a floating-point edited receiver and an all-fixed-point set.** The round-1 shape sent
every floating-point EDITED receiver through a binary64 intermediate, but such a receiver takes the sender's DECIMAL
value and truncates it to its own significand (§14.6.8.4 2), so an exact-valued function lost every digit past the
seventeenth: `MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO D (I)` into `PIC +9.9(20)E+99` stored
`+1.23456789012345670000E+00` under `>>TURN EC-BOUND CHECKING ON` where the un-frozen MOVE stores
`+1.23456789012345678901E+00`, and `MOVE ... TO DQ D (I)` cut both. The binder does not know the function's carrier
family (binary64, SDIDI or scaled integer, `IntrinsicRenderer` decides it), so `ReceivingReach.Of` now picks only shapes
that are exact for every family: a floating-point set holding an edited receiver takes the widest floating-point edited
item the PICTURE admits, `+9.9(35)E+9999` (§13.18.40.3 SR15's 36 digits, §13.18.40.4 GR13 b's four exponent digits),
analyzed by `PictureAnalyzer.Analyze`, because truncating to 36 digits first and then to the receiver's changes nothing.
The sibling sweep of the same rule found the fixed-point arm failing the same way: the returned value's own 21 integer
digits were added to the receivers', so `MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO SL (I)` (and `TO SL1 SL2`)
into `PIC S9(3)V9(28)` kept 17 fraction digits in the 38-digit carrier. An all-fixed-point set now takes exactly its
receivers' widest digits (`ReceivingReach.FixedPointOnly`), since each receiver truncates a sub-range of them
(§14.6.8.2 4); any set with a character receiver keeps the returned value's description, which its text image needs.
`ReceivingReach.Float` is renamed `Floating`. The golden gains legs Z, K, V, P, J and W, of which Z, K, P and J print
DIFFER on the round-2 head. DOC-A.1-92 and DESIGN-binder-bound-tree now state the mixed-set limit as the only one, and
name its three cases.
