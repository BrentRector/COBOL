## Entry 103 — 2026-03-17: STRING, UNSTRING, EXIT PERFORM, Ref-Mod Everywhere

Massive session: 6 features implemented, 4 pre-existing bugs fixed, 2 architectural doctrines
codified, 140 tests passing (up from 111 at session start).

**Features implemented:**
1. **Reference modification as first-class expression** — LowerCondition, BindPrimaryExpression,
   arithmetic operand binding all now handle ref-mod via ResolveExpressionLocation.
2. **SEARCH / SEARCH ALL** — grammar (corrected searchAllWhenClause), bound model, binding with
   index extraction from WHEN conditions, linear search lowering, 13 tests including 2D/3D with
   PERFORM VARYING outer loops.
3. **STRING** — grammar already existed, added BoundStringStatement/BoundStringSending, IrStringStatement
   composite IR, StringConcat/StringConcatLiteral runtime, EmitStringStatement with shared pointer
   local, ON/NOT ON OVERFLOW branching.
4. **UNSTRING** — mirrors STRING architecture exactly: IrUnstringStatement, UnstringExtract per-INTO
   runtime step, shared pointer local, overflow OR'ing, COUNT IN / DELIMITER IN / TALLYING.
5. **EXIT PERFORM** — BoundExitPerformStatement, _performExitStack in Binder, dead block after jump.
6. **Alphanumeric field-vs-field comparison** — IrStringCompare IR instruction,
   CompareFieldToField runtime, category-based dispatch in LowerCondition.

**Pre-existing bugs fixed:**
1. **EmitExpression bypassed IrLocation** — used GetStorageLocation directly for COMPUTE/DIVIDE
   expressions. Fixed with pre-resolved location dictionary on IrComputeStore.
2. **BindPrimaryExpression dropped subscripts/ref-mod** — used IDENTIFIER().GetText() instead of
   BindIdentifierWithSubscripts. Same bug in 4 arithmetic operand binding sites.
3. **Group OCCURS child step size** — ResolveLocation used leaf element size for multipliers
   instead of OCCURS group element size. VAL(2) in a 2-byte group computed offset 1 instead of 2.
4. **Multi-dimensional SEARCH index extraction** — FindSubscriptOnTable took the first subscript
   instead of the one matching the SEARCH table's OCCURS level.

**AI win:** Caught non-standard COBOL in user-supplied 2D/3D SEARCH tests. SEARCH only iterates
the innermost dimension; outer dimensions require PERFORM VARYING. Stopped and asked before
implementing, kept the compiler spec-compliant.

**Architectural doctrine codified:** Four patterns (rogue paths, instance vs pattern, bolting vs
integrating, missing dispatch points) analyzed across 15 entries and formalized in PROMPT.md as
binding development rules for all future sessions.

29 new tests, 140 total, 1 skip, all green.

---

