## Entry 235 — Arithmetic binders diagnose instead of crashing on an undefined receiving item

While starting the IC228A work (nested-program GLOBAL visibility), found a crash-robustness bug: a
trivial `ADD 1 TO NOPE` with an undefined target threw an unhandled `InvalidOperationException`
("ADD statement has no targets") and aborted the compiler — for IC228A this fired on
`ADD 10 TO GLO-DATA-4` (an inherited GLOBAL the binder can't yet resolve). Root cause:
`ExpressionBinder.BindDataReferenceWithSubscripts` silently treats an unresolved data-name as an
alphanumeric literal (no diagnostic, ExpressionBinder.cs:627); the arithmetic binders then found zero
valid targets and `throw`-ed a defensive assertion that is actually reachable from ordinary bad input.

Fix (`ArithmeticStatementBinder`): replaced every defensive `throw new InvalidOperationException(...)`
across `BindMultiply`/`BindAdd`/`BindSubtract`/`BindDivide`/`BindCompute` (15 sites, incl. the
`?? throw` CORRESPONDING fallbacks) with a single `ReportInvalidArithmetic(verb, line)` helper that
reports the new **COBOL0415** diagnostic and returns null. The five binders now return
`BoundStatement?`; the caller (`BoundTreeBuilder.BindStatement`) already null-checks and skips, so the
malformed statement is dropped and compilation reports a clean error instead of crashing. A compiler
must never throw on user input — this is the "scan all similar / fix the pattern" rule applied to the
whole arithmetic-binder family, not just the one ADD site.

Verified: `ADD 1 TO NOPE` now emits `error COBOL0415` and "Compilation failed." (no stack trace);
IC228A likewise fails gracefully (its GLOBAL refs still don't resolve — that's the separate nested-
GLOBAL feature, below). Regression test added
(`ArithmeticTests.Add_UndefinedTarget_DiagnosesInsteadOfCrashing`). Full guard ALL GREEN
(1000 unit / 346 integration / 151 NIST).

Note: this does NOT make IC228A pass. IC228A needs nested-program GLOBAL data visibility — a
contained program referencing a containing program's `IS GLOBAL` item, with the storage SHARED
between them at runtime (IC228A-1 does `ADD 10 TO GLO-DATA-4`; IC228A then checks GLO-DATA-4 = 11).
That is a substantial architectural feature: the compilation pipeline currently flattens nested
programs and compiles each with an isolated symbol table and its own static `State`. A spec-correct
implementation needs (a) containment tracking, (b) data-name resolution that falls back to ancestors'
GLOBAL items, and (c) shared runtime storage (e.g. routing GLOBAL 01-records through the existing
EXTERNAL shared-array mechanism with a family-scoped key, with the inherited items laid out in each
contained program at non-colliding offsets). Scoped as the next focused effort; not started here to
avoid rushing an invasive change into the green baseline.

