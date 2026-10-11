## Entry 359 — M2 (WS-2002-FORMAT): `>>SOURCE FORMAT IS FREE|FIXED` compiler directive

Continuing WS-2002-FORMAT (roadmap §4's #1 M2 workstream). After `*>` inline comments (358), the next `>>`
directive: `>>SOURCE FORMAT IS {FREE|FIXED}` (ISO §7.2) — the spec-mandated explicit selector for reference
format, which until now CobolSharp only ever *guessed* via the `IsFixedForm` structural heuristic.

There was **no `>>` directive handling at all** in the preprocess pipeline
(`Compilation.Preprocess`: read → StripNistArchiveMarkers → NormalizeToFreeForm → CopyProcessor → Nist).
Added it inside `ReferenceFormatProcessor`:

- A compiled regex `^[\s\d]*>>\s*SOURCE\s+FORMAT\s+(?:IS\s+)?(FREE|FIXED)\s*\.?\s*$` (case-insensitive; the
  `[\s\d]*` prefix absorbs any fixed-format sequence area/indenting before the `>>`).
- `DetectDeclaredFormat` returns the format the **first** such directive selects (Auto if none).
- `StripSourceFormatDirectives` blanks every directive line (consumed by the preprocessor; blanked rather than
  deleted so downstream source-line numbers stay aligned for diagnostics).
- `NormalizeToFreeForm` now: if a format is declared, honor it (FIXED → ConvertFixedToFree on the stripped text,
  FREE → pass the stripped text through) — **overriding** the heuristic; otherwise fall back to the existing
  `IsFixedForm` heuristic.

**Near-zero blast radius:** the new behavior is gated on the directive's *presence*. Verified zero existing
source files (NIST or fixtures) contain `>>SOURCE FORMAT` or any `>>` line, so all 364 NIST baselines take the
unchanged heuristic path.

Tests — both chosen to actually exercise the override, not just smoke-test:
- `SourceFormatDirective_Fixed_OverridesHeuristic`: a **blank-sequence** program (no numeric columns 1-6, which
  the heuristic would classify FREE) with a **column-7 `*` comment**. In free form that line tokenizes as code
  and fails; `>>SOURCE FORMAT IS FIXED` forces fixed processing so the comment is recognized → `OK`.
- `SourceFormatDirective_Free_IsRecognizedAndStripped`: column-1 free-form body under `>>SOURCE FORMAT IS FREE`
  → `FREE` (also confirms basic free-form source already parses once the directive line is consumed).

Limitation (documented follow-up): only a single whole-file declaration is honored — the first directive wins;
mid-file format switching, and full dialect-gating of `>>` directives to cobol2002+, remain WS-2002-FORMAT work.

Guard ALL GREEN: **1047 unit / 439 integration / 364 NIST**, 0 regressions.

