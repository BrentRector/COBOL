## Entry 2097 — 2026-10-16 11:30 PDT — PB3233: a figurative INSPECT pattern is SR3's one character, and SR6 / SR9 measure it

**What was wrong.** ISO §14.9.22.3 SR6 and SR9 require literal-1 / literal-3 and literal-4 / literal-5 to be the
same size unless the REPLACEMENT is a figurative constant, and SR3 makes a figurative literal-1, -2 or -4 "an
implicit one character data item". The two size checks in `StatementValidation` returned early unless BOTH
operands were `BoundStringLiteral`, and `InspectBinder.BindPhraseOperands` deconstructed the pattern's
`(Op, Figurative)` answer with `_`. A figurative or symbolic-character pattern was therefore never measured.
Re-probed on this worktree's build: `INSPECT X REPLACING ALL SPACE BY "QR"` compiled and left `A B C ` unchanged,
`INSPECT Y CONVERTING SPACE TO "QR"` compiled and printed `AQBQCQ`, `ALL BLANK-CH BY "QR"` (a bare symbolic
character, §12.3.7.4 GR11 a)) compiled, and only `ALL " " BY "QR"` drew COBOLNET0846.

**Fix shape.** SR6 and SR9 are one rule over two pairs, so the two methods became one,
`StatementValidation.CheckInspectLiteralSizes`. It takes both operands as the binder's tuple, and its helper
`InspectLiteralSize` gives a figurative pattern one character (SR3), a nonnumeric literal its length, and an
identifier, function-identifier or refused operand no static size, which leaves those to the run-time GR14 /
GR22 check. Only a figurative replacement is exempt. The sibling arm, the run-time EC-RANGE-INSPECT-SIZE check in
`CobolInspect`, already measured a figurative pattern as one character against an identifier-5 / identifier-7
(probed with checking on: `ALL SPACE BY R2` and `CONVERTING SPACE TO R2` both end fatally on the EC), so only the
static arm was wrong. `docs/COBOLNET_STRING_OPS_DESIGN.md` now states the rule, and a stale sentence that called
PB1414 open and named the deleted `InspectFigurativeChar` was corrected.

**Goldens.** The positive `conformance:85/pb3233_inspect_figurative_pattern_size` checks equal-size figurative
patterns (SPACE BY "Q", CONVERTING SPACE TO "R", a symbolic character BY "-") and the exempt figurative
replacements ("AB" TO SPACE, "AB" BY ZERO), with values derived from GR14, GR17 and GR22. Three negatives reject
at all four editions with COBOLNET0846: `pb3233-inspect-figurative-literal1-size`,
`pb3233-inspect-figurative-literal4-size` and `pb3233-inspect-symbolic-literal1-size`.

**Rows.** SR-14.9.22.3-6 and SR-14.9.22.3-9 were CONFORMS while under-rejecting. They are re-verdicted on the
new site, the deleted methods are retired as witnesses, and SR-14.9.22.3-3 gains the new witnesses. GAP 78 → 78.

