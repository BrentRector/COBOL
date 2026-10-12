## Entry 386 — M2-DATA-4: BOOLEAN data — `PIC 1`, `USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/compare/JUSTIFIED (ISO §13.18.40.4, §8.3.3.4, §14.6.8.6, COBOL-2002)

Implemented COBOL-2002 **boolean data** as a complete, non-corrupting capability, following the National build
(DEVLOG 383) and the recipe recorded in plan §3.2 (DEVLOG 385). A boolean item (`PIC 1(n)` implicitly, or
explicit `USAGE BIT`) holds boolean positions; stored **one byte per position as ASCII `'0'`/`'1'`** — spec-
permitted (§13.18.40.4 R14: a boolean char "may be represented … as an alphanumeric character"); true
bit-packing (§8.5.1.6.3) deferred. So boolean ≈ "alphanumeric with `'0'` fill + a distinct category".

**Surface (each layer together):** `CobolCategory.Boolean` + `IsBooleanLike()` + `UsageKind.Bit`;
`PicDescriptorFactory` `case '1'` + a `hasBooleanChars` flag + category lattice (sizing = 1 byte/position);
`PicUsageResolver` (`isBool`, `'1'` in the PIC-symbol whitelist, `"BIT"`→`UsageKind.Bit`); `usageKeyword += BIT`
(+ `BIT` in the two `cobolWord` lists so it stays a data-name); lexer `BIT` token + `BOOLLIT : 'B' '"' [01]+ '"'`
(before IDENTIFIER, maximal-munch safe — corpus-clean); `nonNumericLiteral += BOOLLIT`; binder + VALUE-clause
BOOLLIT extraction; `CategoryCompatibility` `(Boolean,Boolean)` move + `IsBooleanFamily` + comparison case.
Runtime: `MoveBooleanToBoolean` (`'0'` fill / right-truncate / JUSTIFIED), `MoveStringLiteralToBoolean`,
`MoveBooleanLiteralToOccursField` (VALUE), `CompareBoolean`/`CompareBooleanToString` (`'0'` pad); emit dispatch
in `CilDataEmitter` (MOVE field + literal), `CilComparisonEmitter` (field=field / field=literal), `CilEmitter`
(VALUE-init), and INITIALIZE-default = boolean zero in `DataMovementLowerer`. DISPLAY needs no change — `'0'`/`'1'`
bytes ride the alphanumeric branch and `TrimEnd` is a no-op on digits.

**A 2-agent adversarial review (the National-383 lesson — verify the whole dispatch surface) caught three real
defects, all fixed before commit:**
- **VALUE corruption (§13.18.63 GR10).** A non-boolean VALUE (`"AB"`, `12`, `SPACE`, `HIGH-VALUE`) was silently
  stored into a boolean item. `DataItemClassifier.ValidateValueClause` now requires a boolean VALUE to be `'0'`/
  `'1'` positions or figurative ZERO (CBL1002 otherwise).
- **Silent-numeric COMPUTE operand.** A boolean nested in a COMPUTE expression tree (`COMPUTE N = B + 1`) was
  read as the decimal 1010 — the operand check only inspected the top-level node. `ArithmeticTypeSystem` now
  recurses the tree and rejects a boolean leaf (CBL2601); scoped to boolean so the pre-existing alphanumeric
  leniency is untouched (a noted follow-up).
- **JUSTIFIED wrongly rejected (§13.18.32.3) + dead code.** `JUSTIFIED` is legal on boolean/national/alphabetic;
  `ValidateJustified` had allowed only alphanumeric, which both violated the spec and left the new
  `MoveBooleanToBoolean` JUSTIFIED branch (and the National `WriteNationalChars` one) unreachable. Now allowed for
  boolean + national — verified `PIC 1(4) JUSTIFIED RIGHT` ← B"11" = `0011` and `PIC N(4) JUSTIFIED RIGHT` ←
  N"HI" = `  HI`. This un-deadens the national branch too (a bonus pre-existing fix).

Verified end-to-end (throwaway + conformance): literal/field/truncation/`'0'`-fill MOVE, VALUE, MOVE ZERO,
INITIALIZE, DISPLAY, comparison (field/literal/eq/ne), JUSTIFIED RIGHT; and clean rejection of non-boolean VALUE,
boolean-in-arithmetic, and non-binary `B"…"`. Conformance `tests/conformance/2002/boolean_data` (12 assertions).
**Deferred (documented, non-corrupting):** the bit operators B-AND/B-OR/B-XOR/B-NOT (reserved-word collision —
`B-NOT` is used as an identifier; needs the XOR-operator wiring pattern), `BX"…"` hex, true bit-packing,
GROUP-USAGE BIT, boolean-vs-non-boolean comparison strictness, boolean ref-modification, the broader alphanumeric/
national-in-COMPUTE latent hole, and the uninitialized-boolean=0x20 default (matches the National precedent).

Guard ALL GREEN (1047 unit / 476 integration / 364 NIST), 0 regressions.

