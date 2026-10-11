## Entry 1273 — 2026-08-09 18:35 PDT — PB59 family 6: the width dispatch becomes total, X'' becomes one token, and the rendered page overturns a day-old comment

THE WIDTH HALF. KnownWidth had three arms and one caller, and the caller's `is {{ }}` guard read every
null as "skip the §15.26.3 r2 / §15.66.3 r2 one-position screen" — so a six-position group, a
two-position refmod view, and ALL "QQ" all sailed past the substitution-character rule while the plain
two-character literal was rejected: a partial function where the rule needs a total one. The dispatch now
answers every statically-sized shape (group → ImageWidth unless a dynamic-length leaf, dynamic-capacity
table, or ODO subordinate makes the width a run-time fact; ALL → the literal's length per §8.3.3.6.4
GR3c; figurative → 1 per GR3b; literal-length refmod → the view width), the one-position shapes stay
accepted (the PB1 over-rejection guard, probed positive), and KnownWidthTotalityDriftTests pins totality
over the BoundOperand hierarchy — proven in the failing direction, including a stale-assembly lesson:
the restored file kept the .bak's mtime, the incremental build skipped it, and the "green" was a stale
mutant until a touch-rebuild. The CLASS half (OperandCategory) is DEFERRED to family 7 with the reason
written down: giving a group a category moves MAX/MIN result-type resolution, which must be measured
together with the ALPHABETIC channel.

THE LEXER HALF. §8.3.3.2.2 Format 2 was RENDERED at 300 dpi before anything was written: both delimiters
printed, the hex sequence bracketed OPTIONAL — so X'' is legal, and the apostrophe arm's hard-coded `+`
(the quotation arm's R03 fix never swept its twin) split it into IDENTIFIER + a zero-length Format-1
literal, firing 1639 + two 1546s on the WRONG argument. The shared HEX_BODY fragment now serves both
delimiters (the Group-B discipline); SUB_HEXLIT is deliberately absent WITH MEASUREMENT (the
keyword-omitted capture re-lexes in DEFAULT mode — probed — and a hex literal is not a legal subscript),
the decision recorded at the fragment. r03 rows 17/18 pin the apostrophe answers; the negative fixture
pins 1546 citing §15.66.3 rule 3 on the RIGHT argument.

THE RENDER OVERTURN. The same page states "The hexadecimal digits are the basic digits '0' through '9'
and the basic letters 'A' through 'F'" (§8.3.3.1) — refuting the PB59-2 comment written HOURS earlier
that defended CONVERT's lowercase HexVal from the term's literal-format usage. §8.1.3.2 GR3a's case
rules govern compilation-group TEXT (X"4a" in source stays legal via CobolLiteral); runtime DATA takes
the capital-letters definition, exactly as BASECONVERT's r2 did. The lowercase arm is deleted and the
correction is written into the comment it replaces.

Incidental finds, each registered before this entry (rule 8): PB70 — MOVE TB(1:1) with TB a GROUP dies
loud at run time (wrong-stage, found by the SUB_HEXLIT probe); PB71 — ALL N"Q" fails to PARSE (a legal
§8.3.3.6 form; found writing the ALL fixture, which now uses the alphanumeric ALL "QQ" shape).

AR-15.26.3-2 / AR-15.66.3-2 / AR-15.66.3-3 → CONFORMS. GAP 4169 → 4166. Gates: the matrix leg green
(2808/2812 with the only four reds being the fixtures' own header/§ mismatches, then fixed — the .err
files now carry the § the console had stripped); Negative 214/214; Corpus/National/Intrinsic 786/786;
the totality drift test red-then-green; SpecTraceabilityInventory 10/10; work.py 128 well-formed.

