## PB2770: the parse-recovery hints say only what is true

`CobolErrorStrategy.GuessCobolIntent` carried four hints keyed on a word's spelling that won the message wherever the
parse happened to fail at that word, and each claimed a limitation the compiler does not have (kb/Work PB2770, an R2
finding of the PB1754 review). Re-probed on this branch's own build before the fix, every scenario reproduced:
`MOVE 1 SET X TO 2` (MOVE lacks its TO) read COBOL0102 "This SET form may not be supported. Supported forms: …",
`MOVE 1 SEARCH E WHEN …` COBOL0103 "SEARCH statement may not be fully supported.", `EVALUATE X ALSO ALSO X …` COBOL0107
"EVALUATE with ALSO (multi-subject) may not be fully supported.", and BLANK where an entry could end COBOL0101 "BLANK
WHEN ZERO may not be recognized". The four hints and their descriptors are deleted, as DEVLOG 232 deleted
COBOL0104-0106; all four probes now read the plain COBOL0001.

The misplaced-scope-terminator hint (COBOL0306) built the rule it looked for from the word's spelling
(`"END-SEARCH"[4..].ToLowerInvariant() + "Statement"`), so `SEARCH ALL E END-SEARCH` was told its END-SEARCH had no
matching SEARCH statement (the lookup could never match searchAllStatement, nor mcsReceiveStatement or
deleteFileStatement), END-OF-PAGE looked for `of-pageStatement`, and a user data-name spelled END-FLAG at a
parse failure was told it lacked a FLAG statement. The hint is now keyed on the token TYPE: `ScopeTerminator` reads
once from the ATN which `…Statement` rules match each END-x vocabulary word (ISO §14.5.3.2, explicit scope
termination), so END-SEARCH closes both SEARCH rules and a word no statement rule matches is no terminator.
`MOVE 1 TO X END-IF` still reads "END-IF appears without a matching IF statement.". The sibling sweep turned every
other keyword test in the method into a token-type test; hint 19's exclusion compared the text with the symbolic
name `FILE_CONTROL` and so never fired. Hint 14's END- exemption keeps the spelling test on purpose: a word spelled
like a terminator (END-INVOKE is a user word, kb/Work PB758) means the writer meant to close something, so "a period
may be missing" would mislead. `DESIGN-frontend-grammar.md` states the rule.

Witnesses: `ParseRecoveryHintTruthTests` (six refused statements that carry no "may not be" text and no COBOL0306,
two misplaced terminators that name their statement), the negative `pb2770-search-all-end-search-no-when` (all four
editions, COBOL0001), and the two `pb758-invoke-end-invoke-*` negatives re-baselined from COBOL0306 to COBOL0001,
which is what their own headers already said the answer is. No inventory row: no ISO clause governs a diagnostic's
wording, and every trigger is an illegal program refused both before and after.

Branch review round 1 (finding R1-1) found the design doc's new rule overstated: it said every hint is keyed on token
types, while hint 4 (COBOL0304) and hint 14's END- exemption still read a token's text. Hint 4 was probed rather than
documented: it fired when a token's text contained ".MOVE", ".ADD", ".SUBTRACT", ".PERFORM" or ".IF", text no COBOL
word can hold (ISO §8.3.2.1; `MAIN-PARA.MOVE` lexes as three tokens and the separator rule reports COBOLNET2632), so
it fired only on literals: `MOVE W-N "A.MOVE"` and `GO TO "PARA.PERFORM".` both read COBOL0304 "Missing period after
paragraph name — the parser is treating it as a qualified reference.". The hint and its descriptor are deleted, and
`ParseRecoveryHintTruthTests` gains the two literal rows (which now read COBOL0001 and the COBOL0307 guess). The doc
names the one remaining spelling test as what it is, a suppression of the missing-period guess, never a hint added
because of a spelling.
