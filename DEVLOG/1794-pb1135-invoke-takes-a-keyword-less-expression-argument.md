## Entry 1794 — 2026-09-29 19:07 PDT — PB1135: INVOKE takes a keyword-less expression argument

`invokeArgument`'s BY-less arms were `addressIdentifier | dataReference | literal`, so `INVOKE o "M" USING N * 2` was COBOL0307, `USING (N + 1)` COBOL0001 and `USING B1 B-AND B2` a reserved-word error, all legal per §14.9.23.2 (BY CONTENT is optional over the expression forms). The rule now carries `CALL`'s two bare expression arms, after dataReference and literal so `N + 1` keeps the list reading PB130 gave CALL; `OfInvokeArgument` marks the argument an expression so it is passed BY CONTENT. Closes FMT-14.9.23.2; the note stays HALF because the adjudicator's `N + 1`-is-one-argument expectation contradicts CALL's recorded determination and needs one decision for both verbs. GAP 798 to 797.

