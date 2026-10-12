## Entry 178 — 2026-03-31: Condition Name Resolution Fix — 17 FAIL* Eliminated

**The bug**: Qualified and subscripted condition names (88-level items) were always
evaluating to FALSE on subscripted table elements. Root cause was in
`ExpressionBinder.BindDataReferenceWithSubscripts`: condition names (ConditionSymbol)
are not DataSymbol objects, so qualified/subscripted references like
`EQUALS-M OF TABLE-LEVEL-5 OF ... (13)` fell through to a bare string literal,
discarding all subscript and qualification information.

**The fix**: Two surgical changes:
1. `SemanticModel.ResolveQualifiedConditionName` — collects all ConditionSymbol
   candidates (including from the scope's Rejections list for duplicate names), then
   disambiguates via qualification chain walking up the DataSymbol parent hierarchy.
2. `ExpressionBinder.BindDataReferenceWithSubscripts` — after DataSymbol resolution
   fails, tries condition name resolution (qualified or unqualified). When found,
   creates a `BoundConditionNameExpression` with the correct `ParentExpression`
   carrying the subscripts. The lowering path (`ConditionLowerer.LowerConditionName`)
   already used `ParentExpression` when non-null — zero changes needed downstream.

**Results**: NC246A: 14 FAIL* → 0 (49/49 pass). NC250A: 4 → 2 FAIL* (2 remaining are
a separate ALL-literal comparison bug). NC235A: bonus +1. Total: 17 FAIL* eliminated.
Guard baseline dropped from 78 → 61 locked FAIL*.

**Lesson**: The scope's Rejections list is the key to finding duplicate-named symbols.
When COBOL defines `88 B VALUE QUOTE` on three different items, the scope rejects
duplicates. But for qualified resolution, ALL candidates must be considered, not just
the primary. This is a pattern that may apply to other qualified resolution paths.

Also fixed Batch 5 skeleton build errors: three Runtime files
(ScreenAttributeMapper, CursorCodec, TerminalSession) referenced Compiler types
(BoundScreenItem, DataSymbol) which is an invalid cross-project dependency. Stubbed
to empty placeholders until M429 implementation.

---

