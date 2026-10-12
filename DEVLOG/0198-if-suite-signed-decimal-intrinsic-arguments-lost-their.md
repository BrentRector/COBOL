## Entry 198 — 2026-05-28: IF suite — signed-decimal intrinsic arguments lost their fraction (lexer)

`FUNCTION MEAN(10.2, -0.2, 5.6, -15.6)` should be `0.0` but computed `0.2`. Decomposing the
result gave it away: `10.2 + 0 + 5.6 - 15 = 0.8, /4 = 0.2` — the *negative* arguments had lost
their fractional part while the positives kept theirs. The asymmetry pointed straight at the
lexer.

In SUBSCRIPT mode (where intrinsic arguments are captured to preserve comma/space separators),
`SIGNED_INTEGERLIT : [+-] [0-9]+` greedily matched `-15` and left `.6` to lex as a separate
`SUB_DECIMALLIT`. The recursive-descent argument parser read `-15`, then stopped (the next token
was not an operator), orphaning `.6`. A positive `10.2` has no leading sign, so it lexed whole
as one `SUB_DECIMALLIT` — hence positives were fine and only negatives broke.

Fix: a dedicated `SIGNED_DECIMALLIT : [+-] [0-9]+ '.' [0-9]+ | [+-] '.' [0-9]+` placed *before*
`SIGNED_INTEGERLIT`. ANTLR longest-match makes `-15.6` one token rather than `-15` + `.6`.
Threaded through `subToken` (parser), the numeric-literal case in `ParseSubPrimary`
(`decimal.Parse` already allowed a leading sign), and the whitespace-split next-token guard in
`SplitSubscriptTokens`. A standalone `-15.6` argument segment falls through `BindSubscriptSegment`
to the arithmetic parser, so no extra case was needed there.

Verified: `MEAN(10.2,-0.2,5.6,-15.6)=0.0`, `MEAN(3.9,-0.3,8.7,100.2)=28.125`. IF CLEAN 17→18.
Guard ALL GREEN (999 / 336 / 95). Remaining statistical fails are the `(ALL)`-subscript form.

