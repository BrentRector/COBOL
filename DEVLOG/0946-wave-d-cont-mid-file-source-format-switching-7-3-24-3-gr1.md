## Entry 946 — 2026-07-21 12:53 PDT — Wave D (cont.): mid-file >>SOURCE FORMAT switching (§7.3.24.3 GR1) — ReferenceFormatProcessor restructured one-format-per-file → per-segment; validated on the GnuCOBOL corpus

`>>SOURCE FORMAT [IS] {FIXED|FREE}` now switches the reference format MID-FILE (§7.3.24.3 GR1), replacing the
old "first directive wins, whole file" model (the documented WS-2002-FORMAT deferral). This is an every-program
path — `NormalizeToFreeForm` runs on every compiled source — so it was gated carefully and validated against the
external GnuCOBOL corpus, not just the internal battery. Spec anchor: an 18-tool-use spec re-scout of §7.3.24 +
§6.5 (logical conversion) + §7.2.1 (stage ordering).

- **The model** (§7.3.24.3 GR1 + §6.5): SOURCE FORMAT is a stateful line-by-line mode flag resolved in §6.5
  logical conversion (§7.2 Step 1 — BEFORE COPY-replace / conditional compilation, matching our existing pipeline
  order). Each directive partitions the source into a homogeneous-format SEGMENT; the directive line is DISCARDED
  (§6.5 step 1) and the new format governs the NEXT line. `NormalizeToFreeForm` now locates the directive lines,
  splits into segments, converts each in its own format (fixed → `ConvertFixedToFree` threaded with a file-relative
  `lineOffset` so the COBOLNET0902/0903 continuation diagnostics keep file line numbers; free → lines as-is), and
  blanks the directive line to preserve the downstream source-line slot. A continued character-string cannot cross
  a boundary (§7.3.3 SR8c), so per-segment continuation/literal state reset is correct. The initial segment (before
  the first directive) uses our implementor-default STRUCTURAL AUTO-DETECT (`IsFixedForm`) — a documented extension
  over the standard's fixed-form GR2 default (the auto-detect classifies the NIST fixed corpus + free-form
  real-world source; DEVLOG 931). GR4 bootstrap (a leading directive) falls out naturally (empty initial segment).
- **Spec-fidelity fix**: `FORMAT` and `IS` are OPTIONAL words (§7.3.24.2) — the regex wrongly required `FORMAT`;
  now `>>SOURCE FIXED` / `>>SOURCE FREE` are recognized. Deleted the now-subsumed `DetectDeclaredFormat` /
  `StripSourceFormatDirectives` / `DeclaredFormat` enum (singular-pattern).
- **⚡ GnuCOBOL external differential in use** (owner directive — the corpus already caught a reference-format bug):
  ran the full 1323-group differential BEFORE and AFTER. Result: **exactly ONE case flipped — a FIX**
  (`syn_copy:660`: `WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT`, a mid-file-format program GnuCOBOL accepts that we
  previously rejected), **zero regressions** (AGREE_ACCEPT 475→476, WE_ACCEPT_THEY_REJECT 106→106 unchanged).
- **Byte-identity preserved**: characterization 33/33 (the every-program path did not alter any emit); the
  no-directive and top-of-file-directive paths are unchanged (the latter still routes through the same conversion).
- ⚔ A 3-dimension adversarial-review WORKFLOW over this every-program path caught TWO major defects (fixed +
  regression-tested): (a) `SplitLines` used `TrimEnd('\n')`, deleting ALL trailing newlines of a converted fixed
  segment — a BLANK source line terminating a fixed segment was LOST, misaligning the following segment; now it
  drops only the single `AppendLine` artifact newline. (b) the directive regex is end-anchored, so a fixed-form
  `>>SOURCE` directive line carrying a card-image sequence tag in cols 73-80 (past margin R) was NOT recognized and
  the following segment was mis-formatted; `MatchDirective` now truncates at margin R (col 72) before matching.
- Tests: 7 cases in a new `SourceFormatTests` (fixed→free, free→fixed, FORMAT/IS-optional, directive-line blanking /
  line-count, top-of-file backward-compat, + the two defect regressions: trailing-blank-line preservation and the
  cols-73-80 sequence tag) + the existing `FixedFormTests` green; a CLI probe compiled + ran a genuinely mixed
  fixed→free program (`MIXED-OK`).
- ⏭ Residual (noted, not in this change): `>>SOURCE FORMAT` / free-form are a 2002+ introduction and should be
  REJECTED at `--std cobol85` (no VCR row today — a VERSION-matrix gap the spec re-scout flagged); and a copybook's
  own `>>SOURCE FORMAT` (§7.3.24.3 GR5, scoped + reverting on COPY return) rides the separate CC-in-COPY item.

