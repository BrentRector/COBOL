## Entry 201 — 2026-05-28: IF suite — CHAR/ORD off-by-one + LENGTH of nested string functions (FAIL* → 0)

The last four IF FAIL* tests fell to two fixes.

**CHAR/ORD were 0-based; the spec is 1-based (ISO §15.9/§15.36).** `FUNCTION CHAR(37)` must
return the character in *ordinal position* 37 — ASCII code 36, `"$"` — but we returned code 37,
`"%"`. The tell was IF105A: `CHAR(37)` (literal, in a `MOVE`) passed under the program's
STANDARD-2 collating sequence while `CHAR(B)` (B=37) failed — both actually went through the
same runtime, both wrong, but only the direct-comparison test surfaced it cleanly. `CHAR(n)` now
returns code `n-1`; `ORD(c)` (its inverse) now returns code+1. `ORD-MAX`/`ORD-MIN` were already
correct — they return a 1-based *argument-list* position, not a collating ordinal, so they were
left alone. Two unit tests and two integration tests had baked in the off-by-one values; corrected
to the spec-true ones (and added a `CHAR(ORD(x)) == x` inverse check).

**LENGTH of a nested string function returned 0.** `FUNCTION LENGTH(FUNCTION REVERSE("Homer"))`
should be 5, but the bind-time LENGTH folder only understood identifier and literal arguments —
a nested `BoundFunctionCallExpression` matched nothing and fell through to 0. Replaced the inline
logic with a recursive `StaticLength`: REVERSE/UPPER-CASE/LOWER-CASE are length-preserving, so
`LENGTH(f(x)) == LENGTH(x)`, and the helper recurses through them to the literal/identifier base.

Also resolved without direct action: the **IF127A timeout** — an earlier additive-argument or
subscript fix removed whatever degenerate loop it hit; it now completes clean.

Result: **IF FAIL* 4 → 0; CLEAN 37 → 42 / 45.** The remaining three (IF401M/402M/403M) are
*flagging* conformance modules — they only self-compare high-subset functions (`ACOS(1.0) =
ACOS(1.0)`) and emit no CCVS PASS/FAIL report by design; they compile and run rc=0, so there is
nothing to fail and no baseline to capture. Guard ALL GREEN (1000 unit / 336 integration / 95 NC).

