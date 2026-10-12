## Entry 101 — 2026-03-17: Reference Modification as First-Class Expression — Killing Rogue Paths

Made reference modification work everywhere, not just MOVE/DISPLAY. Found and fixed three classes
of bypass bugs that had been silently producing wrong results.

**Bug 1: EmitExpression bypassed IrLocation entirely.** The CilEmitter's `EmitExpression` method
(used by COMPUTE, SUBTRACT GIVING, DIVIDE expressions) went directly to
`_semanticModel.GetStorageLocation(id.Symbol)`, ignoring subscripts and ref-mod completely.
Any COMPUTE expression involving a subscripted identifier was silently reading from offset 0
of the array instead of the correct element.

Fix: IrComputeStore now carries a `ResolvedLocations` dictionary, pre-populated by the Binder
via a new `PreResolveExpressionLocations()` tree walker. EmitExpression looks up pre-resolved
IrLocations and uses `EmitLocationArgsWithPic` + DecodeNumeric — the same path everything else
uses. The direct `GetStorageLocation` call in EmitExpression is dead.

**Bug 2: LowerCondition only handled BoundIdentifierExpression.** The comparison lowering used
`binCond.Left as BoundIdentifierExpression` + `ResolveLocation(leftId)`, which meant
`IF FIELD(2:3) = "BCD"` silently failed — the ref-mod expression wasn't a BoundIdentifierExpression,
so leftLoc was null, and the comparison fell through to the fatal throw. Fix: replaced with
`ResolveExpressionLocation(binCond.Left)` which handles both identifiers and ref-mod.

**Bug 3: BindPrimaryExpression dropped subscripts and ref-mod.** The `BindFullExpression` chain
(used by IF conditions, EVALUATE, and anywhere arithmetic expressions appear) had its own
`BindPrimaryExpression` that did `ctx.identifier().IDENTIFIER().GetText()` → bare
`BoundIdentifierExpression(sym, CobolCategory.Numeric)`. This extracted only the name, hardcoded
numeric category, and completely ignored the subscript/ref-mod parse tree children.

**AI failure: didn't scan for similar patterns.** After finding the `BindPrimaryExpression` bug,
I moved on to testing instead of immediately scanning for other instances of
`ctx.identifier().IDENTIFIER().GetText()`. User had to explicitly prompt: "Scan for other
occurrences of this faulty pattern." The scan found 4 more identical bugs in arithmetic operand
binding (ADD, SUBTRACT, MULTIPLY, DIVIDE operands all used the same `IDENTIFIER().GetText()` →
`BindIdentifierOrLiteral(text)` pattern, dropping subscripts/ref-mod). This is the same failure
as Entry 100's "bolting not integrating" — I keep treating bugs as isolated incidents instead of
structural patterns. **Rule added**: after finding any faulty pattern, immediately grep for all
instances across the codebase. Don't wait to be told.

**7 regression tests added** to lock in the invariant that data references flow through IrLocation:
- IF with subscripted identifier, ref-mod, combined subscript+ref-mod, variable ref-mod
- ADD/SUBTRACT/MULTIPLY with subscripted operands

118 integration tests, 1 skip, all green.

**Development rule formalized: Every bug is a pattern.**

When a structural flaw is discovered, perform a full pattern sweep immediately.

*Trigger:* You find a bug caused by bypassing an abstraction, duplicating logic, or violating
layering.

*Action:* Perform a codebase-wide search for all instances of the same pattern, not just the
one that failed.

*Examples:*
- Found one `IDENTIFIER().GetText()` → search for all of them.
- Found one direct `GetStorageLocation` → search for all.
- Found one place bypassing `ResolveExpressionLocation` → search for all.
- Found one place manually decoding numeric bytes → search for all.
- Found one place doing type-check cascades → search for all.

*Outcome:* You eliminate entire classes of bugs instead of single symptoms.

This matters because every single-instance fix creates future regressions, inconsistent behavior,
and architectural drift. Every pattern fix makes the architecture cleaner and new features easier.
The evidence from this session: IrLocation, ResolveExpressionLocation, EmitExpression,
BindIdentifierWithSubscripts, IrComputeStore pre-resolution — each time the pattern was unified,
the entire compiler got simpler.

---

