## Entry 358 — M2 (WS-2002-FORMAT): floating `*>` inline comments in FIXED reference format

Picked the next M2 item by the roadmap's own priority order, not the surgical-fix worklist:
`docs/MULTIVERSION_ROADMAP.md` §4 ranks the COBOL-2002 workstreams "by foundational value and contained-ness"
and puts **WS-2002-FORMAT first — "foundational and self-contained — do first."** Its most self-contained,
highest-value, lowest-risk piece is the floating `*>` inline comment (ISO §6.2.3), ubiquitous in modern source.
This is a far better use of effort than grinding the intricate floating multi-char-currency *editing* logic
(modest value, real correctness risk) or re-attempting the deep CALL RETURNING LINKAGE gap — both kept as
documented follow-ups.

**What already worked:** the lexer has had `COMMENT_START: '*>' -> skip, pushMode(COMMENT_MODE)` all along
(`CobolLexer.g4:60`), so `*>` is skipped to end-of-line for every dialect. Free-form `*>` therefore already
worked, and the fixed→free converter even *emits* `*>` for its own comment lines.

**The actual gap — reference-format detection.** `ReferenceFormatProcessor.IsFixedForm` began with
`if (sourceText.Contains("*>")) return false;` — i.e. the presence of a single `*>` anywhere forced the whole
file to be treated as free-form. But `*>` is legal in BOTH fixed and free format. A genuine fixed-format file
(numeric sequence area in columns 1-6, consistent column-7 indicators) that merely used inline comments was
misclassified, so its columns 1-6 / 73+ were never stripped and the sequence numbers leaked into the token
stream as stray code.

**Fix.** (1) Removed the `*>` short-circuit; classification is now driven purely by the structural heuristic
(numeric sequence + column-7 indicators + 60% threshold), which already returns free-form for genuinely
free-form source and fixed-form for column-structured source — so a fixed file with `*>` is now correctly
column-normalized. (2) Added a literal-aware `StripInlineComment` that truncates a normal source line at an
out-of-literal `*>` (a `*>` embedded in a string literal such as `"A*>B"` is preserved), applied in the
normal-line path before the cross-line literal-state scan — this also stops an apostrophe inside comment text
from mis-joining a following continuation line. No dialect gate: `*>`-as-comment is already ungated lexer
behavior and is not meaningful as an operator pair in any dialect; full dialect-gated `>>SOURCE FORMAT` remains
the larger WS-2002-FORMAT follow-up. Verified zero NIST/fixture `.cob` files contain `*>`, so the detection
change is corpus-safe.

Two integration tests: `InlineComment_FixedFormat_NumericSequence_IsStripped` (real sequence numbers + trailing
`*>` → `HELLO`, which only compiles once the detector honours the column structure) and
`InlineComment_FixedFormat_PreservesStarGtInsideLiteral` (`DISPLAY "A*>B".  *> cmt` → `A*>B`).

Guard ALL GREEN: **1047 unit / 437 integration / 364 NIST**, 0 regressions.

