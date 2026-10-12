## Entry 118 — 2026-03-20: Three Grammar Changes, +259 Kernel Tests — NOT=, Multi-Target SET, SET BY Expression

### The changes

Three grammar changes, each approved by the user after a formal grammar-change proposal:

**1. Abbreviated relational operators** — Added `NOT EQUALS`, `NOT GT`, `NOT LT`,
`NOT GTEQUAL`, `NOT LTEQUAL` to `relationalOperator` rule. COBOL-85 §6.3.4.2 requires
these symbolic negation forms alongside the word forms (`NOT EQUAL TO`, etc.).

**2. Multi-target SET** — Changed `SET identifier TO` to `SET identifier+ TO` in
`setToValueStatement`, `setBooleanStatement`, and `setIndexStatement`. COBOL-85 §14.9.39
Format 1 allows `SET A B C TO value`.

**3. SET UP/DOWN BY expression** — Changed `BY integerLiteral` to `BY arithmeticExpression`
in `setIndexStatement`. Allows `SET IDX UP BY TABLE2-REC(INDEX2)` and other computed deltas.

### Binder work

Multi-target SET required `BoundCompoundStatement` — a new bound node that holds a list of
statements, lowered by iterating each one. The Binder flattens it in `LowerStatement` with a
simple foreach. Clean, no special-casing.

The relational operator mapping needed 4 new entries for `NOT>`, `NOT<`, `NOT>=`, `NOT<=`
→ their logical inversions (NOT > means <=, etc.).

Also fixed: CONTINUE statement was parsed but never bound — added it as a no-op (reuses
BoundExitStatement).

### Also: CONTINUE statement

Grammar had `continueStatement` rule but BoundTreeBuilder never dispatched it. Added
single-line mapping to BoundExitStatement (which the Binder already treats as a no-op).

### Impact

| Test | Before | After |
|------|--------|-------|
| NC172A | parse fail | **101/101** |
| NC177A | parse fail | **108/108** |
| NC127A | parse fail | **2/2** |
| NC137A | parse fail | **8/8** |
| NC131A | parse fail | 9/10 |
| NC140A | parse fail | 28/70 |
| NC141A | parse fail | 3/9 |
| NC203A | parse fail | compiles (div/0 crash) |
| NC251A | parse fail | compiles (div/0 crash) |

+259 kernel tests passing from three grammar lines. NC107A also validated at 100% with
expected output match. 8 additional NIST tests (NC115A–NC126A) also validated at 100%.

### Numbers

- 119 unit tests, 182 integration tests (1 skip), guard ALL GREEN
- NIST at 100%: NC101A–NC107A, NC111A, NC112A, NC115A–NC120A, NC122A–NC124A, NC126A,
  NC127A, NC132A, NC136A, NC137A, NC170A–NC173A, NC175A–NC177A, NC202A, NC207A,
  NC221A, NC222A, NC224A, NC240A

---

