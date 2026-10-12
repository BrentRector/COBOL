## Entry 1090 — 2026-07-28 17:47 PDT — CA37+CA38: the twins, and the first ECs whose lenient arm the STANDARD spells out

Step 10, the last before CA12. Landed as one commit because the two share `CobolDynTable` and the gate table,
and each is the other's context.

**Both rules validated with `cite.py`, and the finding's clause was one level short** — it writes
"§14.9.39 GR30/GR31"; the rules are in §14.9.39.4. §14.9.39.4 GR31: "This statement shall not be executed
during the execution of a SEARCH statement referring to the same table. If this rule is violated, the
EC-FLOW-SEARCH exception condition is set to exist AND THE SET STATEMENT IS NOT EXECUTED." GR30: growth past the
implementor maximum sets EC-BOUND-TABLE-LIMIT "AND THE CAPACITY OF THE TABLE IS UNCHANGED".

**These are the cleanest case for the owner's checking-OFF rule in the whole batch.** The rule is "lenient
wherever the standard NAMES the outcome" — and here the standard names it in the same sentence as the
condition. Both sites previously threw UNCONDITIONALLY, so a checking-off program aborted where the standard
says it should simply not perform the operation. They now return, and the caller does nothing.

**The twins stay distinct, deliberately, and the design says not to merge them.** CA37 is a PRECISE gate: a
capacity SET is one bound node (`BoundSetCapacity`), and it is the only statement that can violate GR31, so the
guard binds exactly there. CA38 is AMBIENT: a dynamic table grows both from an explicit capacity SET and from an
IMPLICIT receiving reference, and the latter renders inline through `RefReceiving` with no statement-level node
of its own. Merging them would have made one of the two wrong.

**⚠ The RefReceiving out-of-bounds guard the risk register predicted is real, and it only became reachable
today.** `GrowTo` can now DECLINE — that is what GR30's "capacity unchanged" means with checking off — so the
occurrence `RefReceiving` was asked for may still not exist when it returns. Falling through to
`_store[occ-1]` would be an IndexOutOfRangeException: a raw .NET failure on user source, from the one path
where the model already has a benign scratch slot for an unreachable occurrence. Guarded.

**Golden** `2023/ec_flow_search_set_capacity`, registered same commit: a capacity SET inside the SEARCH's own
WHEN body. It asserts BOTH halves of GR31 — the declarative selects (HANDLED=EC-FLOW-SEARCH) and the capacity is
still 3 (CAP-UNCHANGED), which is the "SET statement is not executed" half. Without the fix: abnormal
termination. The check is on the VALUE, not on a DISPLAY of the register: an implicitly-defined CAPACITY
register's picture is implementor-defined (§13.18.38 SR30), so its rendering width is not a spec-derived
expectation and asserting it would bake in an implementor choice.

**Three shapes the golden had to find by compiling**, none of them in the finding: the capacity SET is spelled
`SET <register> TO n` and not `SET CAPACITY OF <table>`; the CAPACITY IN register is IMPLICITLY defined, so
declaring it too is COBOLNET1523 (§13.18.38 SR30); and SEARCH needs INDEXED BY (§14.9.37 SR1).

**Gate:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition+Dyn+Search+Occurs 469/469 · Unit
flags-drift+Table 26/26.

**Not covered, named:** EC-BOUND-TABLE-LIMIT has its gate, its lenient arm and the RefReceiving guard, but no
golden — reaching it needs a table grown past the implementor maximum, which is a very large allocation to
provoke in a corpus program. CA12 is next and closes the batch, co-located with the full pre-merge gate.

