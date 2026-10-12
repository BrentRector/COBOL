## Entry 1178 — 2026-08-05 02:15 PDT — an exception's reachability depended on which verb enclosed it; the fix's own first cut had the same bug one level in

**PB26 landed.** `EcBinder.DirectIntrinsic` decided whether to emit the ambient EC-ARGUMENT-FUNCTION checking gate
by a hand-written `switch` over ~17 statement kinds ending in `_ => false`. **§15.3 item 14** attaches that
condition to the FUNCTION REFERENCE — *"If the evaluation of an argument results in an incorrect value for that
argument or for the returned value according to the rules specified in the function definition … the
EC-ARGUMENT-FUNCTION exception condition is set to exist"* — and qualifies on nothing else. No statement kind
appears in the rule at all. So the gate had to reach every statement, and instead it reached the ones someone had
remembered.

**Measured** with `FUNCTION LOG10(0)` (§15.56.3 AR2: the argument "shall be greater than zero") under
`>>TURN EC-ARGUMENT-FUNCTION CHECKING ON`:

| COMPUTE | MOVE | DISPLAY | IF | nested arg | STRING | UNSTRING | PERFORM UNTIL |
|---|---|---|---|---|---|---|---|
| raised | raised | raised | raised | raised | **silent** | **silent** | **silent** |

The identical reference, a different verb. The entry's own words were right and now have a citation: *an
exception's reachability must not depend on which statement encloses it.*

## ✅ THE LIST BECAME A STRUCTURE

The container walk had already been fixed this way once — `BoundStatementTree.StatementChildren` is generated from
the Roslyn semantic model by reading every property of every statement leaf, and its comment records that it
replaced a hand-list that "missed SEARCH/keyed/WRITE/… phrase bodies". The same technique now generates
**`OwnValueParts`** for the value side: every `BoundExpr`/`BoundCondition`/`BoundOperand`/`BoundBoolExpr` a
statement holds directly, through its lists and its helper records. `DirectIntrinsic` is one line over it. A
statement kind added tomorrow is covered without an edit.

## ⛔ AND THE FIRST CUT REPRODUCED THE DEFECT IT WAS FIXING

STRING and UNSTRING started raising; **PERFORM UNTIL did not.** A `[BoundNode]` root is not automatically a
VALUE — `BoundPerformControl` and `BoundSetTarget` are CONTAINERS that HOLD values. The walk yielded the container
and stopped, `EcBinder` had no arm for a container, and the gate was skipped. **A hand-list replaced by a
structure that quietly terminates in the wrong place is still a hand-list with extra steps**, and it failed on
exactly the shape the golden was written to catch.

The generator now emits a walker per container root, and the terminal/container partition of the seven
`[BoundNode]` roots is pinned by a drift test that fails when a new root appears — so the next root forces a
decision instead of defaulting to silence.

## 🔬 TWO TEST DESIGNS THAT WERE UNSOUND, RECORDED SO NOBODY REBUILDS THEM

I wanted a broad reflection guard: "every statement leaf that holds a value node must yield parts". Both attempts
were wrong, and the second was wrong in an instructive way.

1. **One-level property reflection** said `BoundStringStmt` holds no value node. It holds them two hops away —
   `IReadOnlyList<BoundStringSending>`, then `.Value`. That is the very shape the old switch missed.
2. **Probing an uninitialized node** to detect a missing arm cannot work: the generated `V`/`Vz` helpers are
   null-safe, so an ARMLESS leaf and an ARMED-but-null leaf both yield nothing. My own comment reasoned that a
   real arm would throw — it does not. The probe reported all 44 value-bearing leaves as armless, including
   COMPUTE and MOVE, which demonstrably work.

The guard that shipped is narrow and sound: it injects a real operand through the list-of-helper-records path and
asserts the walk finds it, plus a nested-statement exclusion and the root partition. **Breadth is guarded
behaviourally by the golden**, which runs the violation through all eight statement kinds — the honest boundary,
written into the test file so the next person does not rebuild the unsound version.

⚙ The unit guard was proven in the failing direction before being trusted: disabling the helper-record-list branch
in the generator turned it red, and only that one (the nested-statement guard stayed green, correctly).

## ⚙ THREE MORE POSITIONS THAT REFUSE A FUNCTION-IDENTIFIER (PB45)

Building the probe found them, which is the argument for probes that sweep rather than confirm:
`ADD FUNCTION SQRT(X) TO Y` and `INSPECT … TALLYING … FOR ALL FUNCTION LOG10(X)` are **parse errors**, and
`EVALUATE TRUE / WHEN FUNCTION SQRT(X) > 0` compiles clean and throws at run time. All three are legal under
§8.4.3.1.2 Format 1 in SENDING positions — the DA4/PB17 family again. Filed, not fixed here.

⚠ **And I made rule 1's own mistake while writing that note**: I cited §14.9.24 for INSPECT. §14.9.24 is MERGE;
INSPECT is §14.9.22. `cite.py --check` caught it inside the same session that added the check to my habits —
which is the point of running it on every citation rather than on the ones that feel uncertain.

