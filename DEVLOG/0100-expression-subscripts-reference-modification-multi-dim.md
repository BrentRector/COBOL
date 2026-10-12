## Entry 100 — 2026-03-17: Expression Subscripts + Reference Modification + Multi-Dim OCCURS

Extended the IrLocation architecture to handle expression subscripts (ARR(I+1), ARR(I*J)),
reference modification (FIELD(3:2), ARR(I)(3:2)), REDEFINES+OCCURS, and COMP-3 arrays.

**Expression subscripts**: Changed `IrElementRef.SubscriptLocations` from `IReadOnlyList<StorageLocation>`
to `IReadOnlyList<BoundExpression>`. EmitElementAddress now calls EmitExpression for each subscript,
which handles identifiers, arithmetic, and any expression uniformly. This was a simplification —
removed the need for temp storage allocation entirely.

**Reference modification**: Grammar extended with `refModSpec : arithmeticExpression COLON
arithmeticExpression?` as optional suffix on `identifier`. New `IrRefModLocation : IrLocation`
composes base location (static or element) with runtime start:length. `EmitRefModAddress` evaluates
start/length expressions, pushes base via EmitElementAddress or static offset, computes
`baseOffset + (start-1)` and pushes length. Added `ResolveExpressionLocation(BoundExpression)` as
the single entry point for all lowering methods — handles both BoundIdentifierExpression and
BoundReferenceModificationExpression uniformly.

**AI failure (again)**: Initially bolted reference modification onto LowerMove as another type-check
cascade (`if source is BoundReferenceModificationExpression...`) instead of refactoring to a unified
`ResolveExpressionLocation`. Same pattern as the wrapping hack in Entry 099 — adding special cases
instead of fixing the abstraction. User caught it: "I do not want the simplest modification. I want
the production quality changes." The proper fix was straightforward: one new method
(`ResolveExpressionLocation`) that dispatches on expression type, and LowerMove/LowerDisplay call it
for ALL target/source expressions. This is the third time this session I've chosen the lazy path
over the architectural one.

**Pre-change checklist codified** (to prevent recurrence):
1. Is there a single, canonical dispatch point for this concept?
   If yes → extend it. If no → create it. Never wrap around it.
2. Is the type logic centralized or smeared across call sites?
   If smeared → stop and refactor toward a unified resolver.
3. Am I modifying a leaf (like LowerMove) when the concept is more general?
   If yes → I'm probably bolting, not integrating. Step back.

**Tests**: 6 ref mod tests (constant, with subscript, variable start, rest-of-field, expression
start/length, 2D+refmod), 2 expression subscript tests, REDEFINES+OCCURS test, COMP-3 array test.
119 unit, 111 integration, 1 skip. All green.

---

