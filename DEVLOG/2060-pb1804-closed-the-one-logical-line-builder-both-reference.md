## Entry 2060 — 2026-10-16 04:58 PDT — PB1804 closed: the one logical-line builder both reference formats feed is complete

Wave 1084 H was dispatched on the analysis note PB1804, which proposed ONE `LogicalLineBuilder` for the §6.5 logical
conversion, fed by a per-line classifier for each reference format, so that the floating literal continuation (PB1359),
the continuation-line checks (PB1492) and "a directive line is never a join target" (PB1360) would each be one check of
the builder's state. The note had stayed `open` after all three of its children landed (waves 1001 C and 1004 B), so the
register still listed it as open work.

**Re-probe, this worktree's own build (`--std 2023`).** Every step reproduces as fixed. Free form `"ABC"-` / `"DEF"` and
`N"AB"-` / `"CD"` print `[ABCDEF][ABCD]`; the fixed-form floating continuation prints `[ABCDEF]`; a floating-continued
REPLACE pseudo-text matches (`WORLD HELLO`); a continuation line with no quotation symbol draws COBOLNET2684 (§6.2.3.2
SR6); a national literal continued with the column-7 hyphen draws COBOLNET2685 (§6.3.5 2)); a `>>DEFINE` continued with a
column-7 hyphen or with a floating indicator draws COBOLNET2696 (§7.3.3 SR1) and is not joined. The drift test the design
promised holds structurally: the line-for-line free converter (`ConvertFreeFormMapped` / `ConvertFreeLine`) is gone, and
`LogicalLineBuilderTests` runs each rule through both classifiers. `DESIGN-frontend-grammar.md` §3.6.0 already describes
the builder as it is. Every clause the note cites was re-run through `cite.py --check`.

**Change.** None to code: PB1804 flips to `landed` with `closes_rows: []` (an analysis note; its rows were closed by
PB1359, PB1492 and PB1360), and its body records the re-probe and a "for the next implementer" paragraph.

**Lead found and left for its own note.** An alphanumeric literal left unterminated at the end of a free-form line with no
floating indicator is rejected, as §6.4.2 requires, but only by the parser's unnamed `COBOL0001 unexpected '"'` cascade
(pseudo-text gets the named COBOLNET2449). The builder cannot name it: it runs before conditional compilation, and an
unterminated literal in an omitted `>>IF` branch is legal (§7.3.16.4 3); the probe compiles and runs). The named
diagnostic belongs in the lexer's literal rule; it is in the wave-1084 H report for the registrar.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run `20261015T223531Z-56fd77`; Conformance 6,139/11,711, Unit 33,295/33,667, Characterization 6/36 in leg 1; leg 2 left to CI). Linux gate: the lander's (batched gating).

