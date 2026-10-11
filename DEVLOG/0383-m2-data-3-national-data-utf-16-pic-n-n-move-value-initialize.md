## Entry 383 — M2-DATA-3: NATIONAL data (UTF-16) — `PIC N`, `N"…"`, MOVE/VALUE/INITIALIZE/compare (ISO §13.18.60.4, §8.3.3.5, §14.6.8.5, COBOL-2002)

Implemented COBOL-2002 **national (UTF-16) data** as a complete, non-corrupting capability. A national
item (`PIC N(n)` implicitly, or explicit `USAGE NATIONAL`) is stored as UTF-16LE — **two bytes per
national character position** — and the national space (figurative SPACE / fill) is U+0020.

**The supported slice (each layer built together):**
- **Sizing (one pipeline).** `PicDescriptorFactory.ComputeStorageLength` doubles the byte length for a
  National/NationalEdited category in its DISPLAY arm. Because the compiler-side `PicLayout.Length` is just
  `desc.StorageLength`, this one change propagates through `FieldSizeCalculator`/record layout/`StorageLocation`
  with no second size path. (Confirmed: **no NIST program uses `PIC N`**, so this disturbs no baseline.)
- **Literal.** New lexer token `NATLIT : 'N' '"' … | 'N' '\'' …` (before HEXLIT; `caseInsensitive=true`
  covers `n"…"`; maximal-munch keeps `NAME`/bare `N`/the string `"N"` unaffected). Added to
  `nonNumericLiteral`. `ExpressionBinder` strips the `N` + quotes, un-escapes doubled quotes, and binds
  `CobolCategory.National`.
- **`USAGE NATIONAL`** added to `usageKeyword` only (reachable solely via `USAGE IS?`, so `NATIONAL` is still
  usable as a data-name and `… FOR NATIONAL` still parses); `UsageMapper` → new `UsageKind.National` (inert —
  the category drives all behavior; every `UsageKind` switch has a safe default and national items never reach
  the numeric/binary arms).
- **Runtime (UTF-16, char-aware).** `WriteNationalChars` (the shared left/right-justify + national-space pad +
  right-truncate primitive); `MoveNationalToNational`, `MoveStringLiteralToNational`; `GetDisplayString`
  decodes National via `Encoding.Unicode`; `CompareNational` reconstructs code units little-endian and pads
  the shorter side with U+0020 (correct ordering for code points ≥ U+0100, unlike a byte-wise compare).

**The adversarial review caught that the first slice silently corrupted common operations** (transparency —
a 3-agent review verified each by running probes; the conformance test had been too narrowly scoped to the
working paths). Every legal national operation now routes through char-aware code:
- **national ↔ alphanumeric / numeric MOVE** (ISO Table 16). `MoveAlphanumericToNational` (Latin-1 widen,
  high byte 0), `MoveNumericToNational` (display digits then widen), `MoveNationalToAlphanumeric[Edited]`
  (narrow; non-Latin-1 → '?'). `CilDataEmitter.EmitMoveFieldToField` now dispatches a national receiver
  (national/numeric/alphanumeric source) and a national source into an alphanumeric receiver — was falling to
  the byte-copy `MoveAlphanumericToAlphanumeric` and producing embedded NULs / mis-read UTF-16.
- **MOVE SPACE / figurative / INITIALIZE → national.** `MoveFigurativeToField` gained a National arm that
  fills each position with the 2-byte UTF-16 figurative char (SPACE=U+0020, ZERO=U+0030, HIGH=U+FFFF,
  LOW/NULL=U+0000) — single-byte fill had produced U+2020 etc.
- **VALUE clause on a national item.** `SemanticBuilder` now extracts a `NATLIT` (and a plain `STRINGLIT`)
  VALUE; `CilEmitter.EmitValueClauseInitialization` routes a national item to new
  `PicRuntime.MoveNationalLiteralToOccursField` (UTF-16) instead of the 1-byte ASCII occurs-fill.
- **National comparison.** `CilComparisonEmitter` dispatches at emit time on the field's PIC category:
  national-vs-national → `CompareNational`; national field vs literal → new `CompareNationalToString`
  (`IF n = N"ABC"` had always been false — field "A\0B\0C\0" vs "ABC"). No new IR nodes needed.

Verified end-to-end (throwaway + the conformance program): exact fit, right truncation (proves 2-byte
sizing — a 1-byte layout would keep one char), national-space fill clearing residue, field/literal/numeric/
figurative MOVE both directions, VALUE, INITIALIZE, and `=`/`<` comparison.

**Deferred (documented, not corrupting the supported subset):** NATIONAL-EDITED (the `MoveNational*Edited*`
delegations remain; not reachable — no PIC-N-edited parsed), `NX"…"` hex national literals, the full
implementor character-correspondence + `EC-DATA-CONVERSION` (Latin-1 subset only), collating-sequence-aware
national compare, and reference-modification ×2 byte adjustment for national.

Conformance `tests/conformance/2002/national_data` (13 assertions across every path above). Guard ALL GREEN
(**1047 unit / 475 integration / 364 NIST**), 0 regressions. Process note: the adversarial-review step is
what turned a corrupting half-feature into a correct one — a narrowly-scoped conformance test passing is not
evidence of completeness.

