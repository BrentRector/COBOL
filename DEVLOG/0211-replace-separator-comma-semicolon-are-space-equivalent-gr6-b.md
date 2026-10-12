## Entry 211 — 2026-05-29: REPLACE — separator comma/semicolon are space-equivalent (GR6(b); SM208A CLEAN)

REP-TEST-8 (SM208A, citing COBOL-85 XII-7 3.4 GR6(b)) matches `REPLACE ==MOVE;  "FAIL"  , TO==`
against source `MOVE  , "FAIL";      TO`. Per COBOL-85 the separator comma and semicolon are
equivalent to a space in REPLACE/COPY matching — they are NOT text words. I had been emitting
them as standalone text words, so the differing comma/semicolon placement blocked the match.
Restricted `IsSeparatorPunctuation` to the separator period and added
`IsSpaceEquivalentSeparator` (comma/semicolon not followed by a digit), which the tokenizer skips
like white space. `MOVE; "FAIL" , TO` and `MOVE , "FAIL"; TO` now both tokenize to
`MOVE "FAIL" TO` and match. SM208A → CLEAN; SM CLEAN 9→10. Guard ALL GREEN (1000 / 336 / 137).

