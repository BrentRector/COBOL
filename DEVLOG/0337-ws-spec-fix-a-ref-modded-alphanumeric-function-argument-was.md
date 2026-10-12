## Entry 337 — WS-SPEC fix: a ref-modded alphanumeric function argument was read as numeric (returned 0)

In `ExpressionLowerer`'s function-call argument classifier, a `BoundReferenceModificationExpression` argument fell
through to the numeric-default path (`IrNumericArg` → the substring decoded as a decimal → 0 for alphabetic
content), so `FUNCTION UPPER-CASE(X(1:4))` returned `0`. A reference-modification operand is **always category
alphanumeric** (ISO §8.4.2.3): added a case lowering it to `IrAlphanumericArg` (via `ResolveRefModLocation`),
read as a string. CLI-verified: `UPPER-CASE(WS-IN(1:4))` → `ABCD` (was `0`); `LENGTH(WS-IN(2:3))` still `3`. Test:
`SpecFixTests.RefModdedAlphanumericFunctionArg_ReadsAsString`. 7th backlog fix.

