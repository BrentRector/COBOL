## Entry 1360 — 2026-08-29 01:30 PDT — PB124 wave 5b: index FUNCTIONS are class index — typed by the one Resolve, screened at MOVE and every class-numeric argument, and the storage enum deliberately unchanged

GR-15.2-6 / AR-15.3-5 / AR-15.3-10 (batch 7): §15.2 item 6 makes MAX/MIN over index arguments an INDEX
function ("these are of the class and category index"), but the result's STORAGE category folded to Numeric,
so it was indistinguishable downstream: MOVE FUNCTION MAX(IX1 IX2) TO PIC 9(n) silently stored an
occurrence-number image where §14.9.25.3 SR1 bars class index outright, and
FUNCTION SQRT(FUNCTION MAX(IX1 IX2)) passed §15.84.3 r1's class-numeric screen. Both now reject — typed by
the ONE IntrinsicResultType.Resolve the binder already resolves result types with: ClassOf1's nested-call arm
answers CobolClass.Index, and MoveBinder's SR1 arm gained the function-sender twin citing item 6.

Two deliberate boundaries, named so they are not mistaken for gaps. PicCategory gains NO Index member — it
is the STORAGE enum, and the class/type question rides IntrinsicType + CobolClass through Resolve at each
consumer that cares (the 67-file exhaustive-switch churn a storage-enum member would cost buys nothing the
consumers need). And a relation over index operands KEEPS the numeric carriage — §8.8.4.2's index comparison
IS a value comparison of occurrence numbers, pinned RUNNING in the golden (bare, MIN and SUBSCRIPTED forms).
The subscripted leg also measured AR-15.3-5's third claim FALSE — a subscripted index item DOES select the
Index result row (Place.Item reaches through the subscript; only ref-mod is decorated differently, and that
is rejected at its own site). GR-15.2-6, AR-15.3-5, AR-15.3-10 → CONFORMS. PB124's remainder: GR-15.2-2,
AR-15.3-11, AR-15.3-13.

