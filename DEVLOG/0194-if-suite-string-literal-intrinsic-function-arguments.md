## Entry 194 — 2026-05-28: IF suite — string-literal intrinsic-function arguments

Functions like `LOWER-CASE("ABC")`, `UPPER-CASE`, `NUMVAL`, `REVERSE` take alphanumeric
literal arguments. SUBSCRIPT mode had no string-literal token, so the `(` failed to parse
(`no viable alternative`). Added `SUB_STRINGLIT` to SUBSCRIPT mode + `subToken`; `ParseSubPrimary`
binds it as an alphanumeric `BoundLiteralExpression` (un-doubling embedded quotes). Cleared 10
of the 16 IF compile-failures (IF: 3→6 clean). Guard ALL GREEN.

Remaining IF: 6 clean, 24 FAIL* (per-intrinsic value correctness), 6 compile-fail (incl. a
non-numeric function used as a comparison operand — COBOL0504), 9 crash/timeout — a per-function
long tail.

---

