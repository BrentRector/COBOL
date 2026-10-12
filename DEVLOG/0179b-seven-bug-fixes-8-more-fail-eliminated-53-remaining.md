## Entry 179 — 2026-03-31: Seven Bug Fixes — 8 More FAIL* Eliminated (53 remaining)

Seven parallel fixes across binder, lowerer, runtime, and semantic analysis:

**UNSTRING overflow** (NC218A GF-15.01): `UnstringExtract` set overflow=true on source
exhaustion, but ISO 14.9.44 says overflow occurs only when unexamined chars remain
AFTER all INTOs processed. Fixed to return -1 (unacted) instead of overflow flag.
Pre-loop overflow check added in CilStringEmitter for pointer range validation.

**ALPHABET ALSO clause** (NC215A GF-3, GF-4): `BuildAlphabetCollatingSequence` ignored
ALSO entries — only the primary literal got a weight. Fixed to assign identical ordinal
weight to all ALSO literals. Bonus: GF-4 also fixed because it depended on correct ALSO
weights for the I-N group.

**INSPECT keyword inheritance** (NC216A F1-32): Bare patterns in INSPECT TALLYING
inherited ALL by default instead of the preceding keyword (LEADING/FIRST/TRAILING).
Added `lastKind` tracking within each FOR clause.

**RENAMES in Children** (NC252A): Level-66 RENAMES items weren't in parent 01-record's
Children, so qualified references like `RENAME-5 OF T-RENAMES-DATA` fell through to
string literals. Added RENAMES to Children; added level-66 skips in CorrespondingMatcher,
StorageLayoutComputer, RecordLayoutBuilder, and DataMovementLowerer (INITIALIZE).

**Qualified PERFORM** (NC208A PAR-F2-2): `ProcedureNameResolver.ExtractProcedureNameText`
ignored OF/IN section qualifiers. Added `ExtractProcedureNameWithQualifier`, section-scoped
paragraph resolution, symbol-based method/index lookups in Binder and LoweringContext.

**Collating sequence bypass** (NC215A GF-5): `eitherNumeric` shortcut in ConditionLowerer
used `IrPicCompare` regardless of PROGRAM COLLATING SEQUENCE. Now falls through to
`IrStringCompareWithSequence` when collating sequence is active and one operand is
non-numeric.

Results: NC252A 4→1, NC218A 9→8, NC215A 6→3, NC216A 8→7, NC208A 4→3 FAIL*.
Guard: 53 FAIL* locked (was 61 after previous commit, 78 at session start).

---

