## Entry 873 — 2026-07-17 10:45 PDT — P11 Step 2 — FUNCTION BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN: the boolean-result channel becomes REAL (category Boolean rides the string channels); Deferred 12→10

The first P11 promotion wave, executed entirely from the pre-verified scout-notes appendix (the edit list +
golden values were derived and persisted BEFORE implementation — the P10 re-scout discipline paying off: the
golden ran FIRST TRY, byte-exact against the hand-derived `.out`).

**The load-bearing design move** (scout `code:renderer-runtime` §11): a boolean FUNCTION result was
unreachable — `IntrinsicSig.ResultCategory` folded Boolean→Numeric (the documented Deferred-era shortcut),
so no string channel would carry it. Now a boolean function result IS category boolean (§15.2 item 2;
§8.5.2.5 item 4), riding the SAME channels national rides:

- `IntrinsicCatalog.ResultCategory` — `IntrinsicType.Boolean => PicCategory.Boolean` (doc comment rewritten).
- The four seams widened to admit Boolean: `OperandText.AsString` entry intercept · `IsStringVisitor`'s
  computed arm (a boolean result compares as text, §8.8.4.3 over the D-B1 '0'/'1' substrate) ·
  `StrArgVisitor`'s nested arm (a boolean-result argument to another function) · `RenderNum`'s string-class
  guard (a boolean result in a numeric context is loud BY NAME).
- ZERO new mechanism beyond that: `MoveCategoryLegality` already reads `ic.ResultCategory` for computed
  operands (Table-16 legality just works), and `MoveEmitter`'s boolean receiver already stores through
  `StrStoreBoolean(OperandText.AsString(...))` — §14.6.8.6 left-justify/right-zero-fill compose for free.

**Runtime** (`CobolIntrinsics.Text.cs`): `BooleanOfInteger(value, length)` — §15.13.4 r1, rightmost position
= low-order digit, zero-filled or TRUNCATED ON THE LEFT to argument-2 positions (result = value mod
2^length; left truncation is NORMAL — Annex D.10's 544→low-6-bits example); argument-1 = 0 ACCEPTED (the
r1-vs-r2 "positive"/"positive nonzero" drafting contrast, scout-resolved), negatives + out-of-range
argument-2 → EC-ARGUMENT-FUNCTION; the documented §15.4 max returned length = the §8.3.3.4.3 SR1 8191.
`IntegerOfBoolean(boolean)` — §15.45.4 r1 unsigned MSB-first; >63 significant bits → EC via the §15.4 hook
(documented policy; the spec sets no cap); zero-length → 0 (flagged undefined edge).

**Tests:** golden `2002/intrinsics_boolean_conv` (6 probes: exact fit · the D.10 544/6 left-truncate + MOVE
right-fill composition · MSB-first 128 · literal argument · round-trip 200 · 256 mod 2^8 = 0) + negative
`boolean_conv_below_2002` @85 (COBOLNET1502 BY NAME for both) + matrix row `boolean-of-integer-2002`
(145 rows; gen-constructs regen committed) + 2 `AssertSpec` facts + the `GreenfieldOnly` exclusion (frozen
legacy has no boolean intrinsics). Deferred: **12 → 10 rows**. CLI spot-runs verified the VALUES, not just
non-crash. Battery on the staged tree: conformance
**3475/3475** (+8: the golden, the negative row, the 4 matrix cells, 2 spec facts) · unit **292/292** ·
legacy 1196+636 ALL GREEN · guard-fast first pass 352+1 (SQ122A COMPILE FAILED) → **solo rerun 353 MATCH,
0 regressions, ALL GREEN** — the environmental flake class AGAIN (4th occurrence: always a file-I/O-suite
COMPILE FAILED/DIFF under the JOBS=32 parallel guard; the solo-rerun adjudication discipline is now
standing procedure, banner-noted).

