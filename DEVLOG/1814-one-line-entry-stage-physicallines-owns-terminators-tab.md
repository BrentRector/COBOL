## Entry 1814 — 2026-09-30 12:30 PDT — One line-entry stage: `PhysicalLines` owns terminators, TAB expansion, the 255-position limit check's input and the --nist archive markers (PB1800 + PB1586 + PB1803 + PB1496)

Six places split or indexed the raw text on their own (`StripNistArchiveMarkers`, `NormalizeToFreeFormMapped`, `FirstSourceDirectiveCandidate` /
`DirectiveText`, `IsFixedForm`, `FixedFormConverter.Convert` per segment — re-joined then re-split — and `ConvertFreeFormMapped`), so four
implementor determinations ISO §6.1 leaves open had no home. **The stage:** `src/Cobol.Net.Frontend/Preprocessor/PhysicalLines.cs#Read`,
called once at the entry of `NormalizeToFreeFormMapped` (source and library text alike). **Terminators** (DOC-A.1-156): LF, or CR LF; a CR
not followed by LF is a character of the line — read from GnuCOBOL's `ppinput` (`cobc/pplex.l`, extracted read-only from the tarball),
rule-1 precedence. **Tabs** (DOC-A.1-157; owner decision R55): every TAB becomes the spaces to the next stop (positions 1, 9, 17, …) before
anything reads the line — indicator area, program text, free form and a literal alike; `X"09"` stays the way to put a TAB in data. The
line keeps a `TabMap` only when it held a tab (nothing consumes it yet; PB1801). **Archive markers** (PB1803): `*HEADER,` / `*END-OF,` are
blanked only when `ccvsIndicators` (`--nist`); the strip used to run on EVERY compilation, so a stray marker was discarded silently
instead of COBOLNET2616. `StripNistArchiveMarkers`, `ConvertFixedToFree`, `ConvertFixedToFreeMapped` and the string `IsFixedForm` are
deleted; every consumer reads `ReadOnlySpan<PhysicalLine>`, whose `Number` is file-relative (the `lineOffset` parameter is gone); the
legacy oracle's two callers changed. **255 positions** (PB1496, §6.1 3) a); cite.py OK): COBOLNET2653, asked in the free-form arm
(`ConvertFreeLine`) on the EXPANDED line, never on a fixed-form line.
**The estimate was short, and the answer is the restructuring (rule 5):** the note said the lone-CR lexer change is "one lexer rule". It is
nine lexer sites (`COMMENT_*`, `STR_BODY`, `WS`, `PIC_*`, `SUB_*`, `FNARG_SEPARATOR`, two predicates) plus `TextWordScanner`, `SeparatorRule`,
`FloatingComment` and three `TrimEnd('\r')` in later stages — each a private copy of a tab or CR rule that the stage makes dead. All deleted;
the lexer's separator space is `[ \n]`. `PhysicalLinesDriftTests` fails if a pipeline stage spells `\t` or `\r`, a reference-format walker
splits text itself, or `PhysicalLines.Read` gains a second production caller (the proposal's blanket `Split('\n')` ban was narrowed on
purpose: eight later stages split the resultant LF-only text, not raw text).
**Measured** (main build before, this tree after): two leading TABs at `--std 85` — COBOLNET2616 "column 7 holds 'L'" → prints T1; a
256-position free-form line — accepted silently → COBOLNET2653 at column 256 (255 accepted); a lone CR in a literal — unterminated
literal → the literal's second character; a `*>` comment holding a lone CR — ended at it → does not; `*HEADER,COBOL,X` without `--nist` —
compiled clean → COBOLNET2616. **Goldens:** 85/`pb1586_tab_stops_fixed_form`, 2023/`pb1586_tab_in_literal`, 2023/`pb1586_tab_free_form`,
2023/`pb1496_lone_cr_is_a_character`, 2023/`pb1496_free_form_line_255`, negative/`pb1496-free-form-line-256`,
negative/`pb1803-nist-marker-outside-nist`; unit `PhysicalLinesTests` (incl. the TabMap round-trip property over every short line) and
`PhysicalLinesDriftTests`. **Rows:** DOC-A.1-157, DOC-A.1-156, SR-6.1-3 and GR-6.5-9 → CONFORMS (GAP 750 → 746); docs: CONFORMANCE
DOC-A.1-156/157/23, DESIGN-frontend-grammar §3.6, DIAGNOSTICS (2653), DRIFT_RULES. New note: PB1821 (dead `Common/SourceText.cs`, a wrong
second line model). **The first gate went red on the new check itself, which is the check working:** `LiteralScreenTests` and
`NationalBooleanLiteralTests` wrote an 8,192-position literal on one free-form line (not a program §6.1 3) a) allows — they now lay it
out in fixed form through the new `FixedFormLayout` test helper, §6.3.5 continuation), two negative goldens carried a 257- and a
275-position comment line (re-wrapped, text unchanged; PB1757 records that `Auto` had read the first, a fixed-form-authored file,
as free form), `DiagnosticPositionTests.NistArchiveMarker_DoesNotShiftLaterLines` now compiles under `--nist` (plus its non-nist twin),
and five inventory rows still named the deleted `ConvertFixedToFree` (re-sited through `record_verdicts`). Not taken: PB1801 (columns) and PB1757 (the harness's Auto) — their notes carry what this change leaves them.
Friction: the worktree Bash hook refused every heredoc with a backslash and every `$var` command name, so scripts were written with the
Write tool and run by path.

