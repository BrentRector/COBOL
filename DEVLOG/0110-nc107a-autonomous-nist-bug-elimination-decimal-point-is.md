## Entry 110 — 2026-03-19: NC107A + Autonomous NIST Bug Elimination — DECIMAL-POINT IS COMMA, Unified Arithmetic Architecture

The first session driven by PROMPT2.md — autonomous NIST test-driven bug elimination with minimal
user intervention. Started on NC107A (the hardest kernel test so far), then swept through NC108M–NC125A.
Every bug fix was gated by guard.sh (119 unit + 164 integration + 10 NIST golden-file = 964 kernel tests).

### NC107A: From 0/177 to 166/177

NC107A tests figurative constants, continuation lines, separators, JUSTIFIED RIGHT, SYNCHRONIZED,
BLANK WHEN ZERO, max-length names/literals, REDEFINES, USAGE, VALUE for OCCURS, CURRENCY SIGN IS "W",
DECIMAL-POINT IS COMMA, numeric paragraph names, and CONTINUE. The hardest NIST kernel test yet.

**DECIMAL-POINT IS COMMA** — the classic COBOL chicken-and-egg problem. SPECIAL-NAMES configures
how numeric literals are lexed, but SPECIAL-NAMES is parsed *after* lexing. My first attempt
followed the user's purist guidance: remove DECIMALLIT from the lexer entirely, parse numeric
literals in the parser via `numericLiteralCore: INTEGERLIT decimalPoint INTEGERLIT`. This was
architecturally clean but **catastrophically wrong** — DOT is ambiguous between decimal point
and sentence terminator, and ANTLR's greedy matching consumed `30.01` across statement boundaries
(the DOT after `VALUE 30` was swallowed as a decimal point with the `01` on the next line).
44 integration tests failed instantly.

**The fix**: keep DECIMALLIT in the lexer for DOT-based decimals (maximal munch resolves the
ambiguity correctly) but handle COMMA-based decimals in the parser. Split the lexer COMMA rule:
`COMMA_SEP: ',' [ \t\r\n]+ -> skip` (comma-space separator) and `COMMA: ','` (standalone comma
visible to parser). Parser rule `numericLiteralCore: DECIMALLIT | INTEGERLIT COMMA INTEGERLIT |
COMMA INTEGERLIT | INTEGERLIT`. This is the pragmatic hybrid: DOT disambiguation stays in the
lexer where it works, COMMA disambiguation lives in the parser where DECIMAL-POINT IS COMMA
requires it. Zero regressions.

**Numeric paragraph names** — NC107A uses `3.`, `4.`, `5.`, and 25-digit numeric section names.
Added `procedureName: IDENTIFIER | INTEGERLIT` and propagated through paragraphName, sectionName,
GO TO, PERFORM, PERFORM THRU. The scope of the change was larger than expected — goToStatement
had to switch from `identifier` to `procedureName` for targets while keeping `identifier` for the
DEPENDING ON selector.

**OCCURS VALUE initialization** — 99 of 177 failures were from a single bug: VALUE clauses on
OCCURS items only initialized the first element. `MoveStringToField(area, 0, 20, "AZ")` wrote "AZ"
at bytes 0-1 and spaces at 2-19 instead of replicating "AZ" across all 10 slots. Added
`MoveStringToOccursField` runtime helper and OCCURS-aware CIL emission with nested parent
flattening (walks parent chain, multiplies contiguous OCCURS counts for 2D+ tables).

**JUSTIFIED RIGHT truncation** — two bugs. Field-to-field MOVE kept leftmost chars when source >
target (should keep rightmost per ISO §13.16.35). String-literal MOVE bypassed JUSTIFIED entirely
via `StorageHelpers.MoveStringToField`. Fixed both: `MoveAlphanumericToAlphanumeric` now handles
source > target correctly, and added `MoveStringToJustifiedField` + CilEmitter routing.

**USAGE inheritance** — `02 U5 USAGE IS COMPUTATIONAL` didn't propagate to children without
explicit USAGE. Added `HasExplicitUsage` flag on DataSymbol, inheritance in `AddChild`.

**BLANK WHEN ZERO + VALUE clause** — `EncodeDisplay` applied BLANK WHEN ZERO during VALUE
initialization, blanking `PIC 999 VALUE "000"` to spaces. Added `suppressBlankWhenZero` parameter
to `EmitLoadPicDescriptor` for VALUE init path.

### Unified Arithmetic Grammar

The user drove a production-grade grammar refactoring across all arithmetic statements. Key insight:
COBOL-85 has a single rule — "in any GIVING form, the receiving operand may be a literal" — that
applies uniformly to ADD, SUBTRACT, MULTIPLY, and DIVIDE. Instead of patching each statement:

- `givingReceiver: identifier | literal` — one rule, one source of truth
- `arithmeticTarget: identifier ROUNDED?` — replaces addTarget, subtractTarget, divideTarget
- `arithmeticOnSizeError` — replaces 4 identical per-statement SIZE ERROR rules

`divideIntoOperand` is the one exception: uses `arithmeticTarget | literal` (not `givingReceiver`)
because the non-GIVING INTO form needs ROUNDED support.

### Unified BoundArithmeticStatement

Replaced 5 separate bound node types (BoundAddStatement, BoundSubtractStatement,
BoundMultiplyStatement, BoundDivideStatement, BoundComputeStatement) with a single
`BoundArithmeticStatement` discriminated by `ArithmeticKind`. Net -63 lines. Properties:
Operands, Receiver (the TO/FROM/BY/INTO operand), Targets, IsGiving, IsByForm, RemainderTarget,
SizeError. Binder's `LowerArithmetic` dispatches by kind to existing per-op lowering methods.

### Conformance Test Suite

Added 11 integration tests covering the arithmetic GIVING-form literal matrix plus OCCURS VALUE,
JUSTIFIED RIGHT, USAGE inheritance, BLANK WHEN ZERO, and DECIMAL-POINT IS COMMA. These prevent
regression on every fix from this session.

### What Broke and Why

1. **Removing DECIMALLIT** — DOT ambiguity. ANTLR's greedy `INTEGERLIT DOT INTEGERLIT` consumed
   sentence-terminating DOTs as decimal points. Reverted to hybrid: DECIMALLIT for DOT, parser for COMMA.
2. **goToStatement identifier → procedureName** — broke ReferenceResolver and BoundTreeBuilder which
   expected `ctx.identifier()` arrays. Fixed by switching to `ctx.procedureName()` and separating
   the DEPENDING ON identifier.
3. **divideIntoOperand: givingReceiver** — lost ROUNDED support for non-GIVING `DIVIDE INTO B ROUNDED`.
   Fixed: `arithmeticTarget | literal` instead of `givingReceiver`.

### NIST Sweep Results

| Test | Pass/Total | Notes |
|------|-----------|-------|
| NC107A | 166/177 | 6 remaining: 4 continuation, 2 REDEFINES size |
| NC111A | 7/7 | 100% |
| NC112A | 31/32 | SUBTRACT FROM literal works |
| NC119A | 30/36 | |
| NC120A | 31/39 | |
| NC124A | 158/169 | |
| NC117A | compile ok, runtime divide-by-zero (pre-existing SIZE ERROR gap) |
| NC108M | skip — needs implementor switch names |
| NC109M | 1/11 — ACCEPT FROM DATE issues |
| NC115A | 13/31 — INSPECT TALLYING+REPLACING combined runtime issues |

---

