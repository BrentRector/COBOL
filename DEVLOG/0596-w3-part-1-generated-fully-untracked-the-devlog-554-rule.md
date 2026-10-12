## Entry 596 — 2026-07-03 21:55 PDT — W3 part 1: Generated/ fully untracked (the DEVLOG-554 rule RETIRED) · the PIC_STRING separator cure (VCR 7.14 FIXED) · the XOR/EXCLUSIVE-OR 2023 regating (VCR 41 GATED)

**Three of the six W3 items landed as the serialized grammar batch (the remaining three — the notInGrammar
85-acceptance set, preprocessor DialectLevel threading, the 2002-corpus audit — carry to the next session).**

1. **Generated/ untracked — the DEVLOG-554 rule is RETIRED.** The regen has been path-portable + fail-hard
   since DEVLOG 555/556, but 8 generated files were STILL TRACKED — on a fresh checkout their mtimes SUPPRESS
   regeneration (GenerateIfNewer compares LastWriteTime), which is exactly how the d6c8143 stale-TYPE-gate
   incident happened (DEVLOG 591). `git rm --cached` × 8; CI now always regenerates (java + pwsh are on the
   GitHub runner images); there is no committed parser to keep in sync anymore. CI workflow comment updated;
   the resume-prompt CI NOTE retired.
2. **The PIC_STRING trailing-separator cure (VCR Table 7 row 7.14 → FIXED).** The lexer action now trims a
   trailing `,` or `;` ONLY when LA(1) is whitespace/EOF — the §8.3.5 r2 separator shape. The LA-guard is
   load-bearing and was proven by the first attempt FAILING: NC125A's `PIC 9,9,…,9,.` is the LEGAL SR7
   trailing-comma mask (PICTURE as the last clause before the period) — the token ends at the ',' because
   '.'+newline cannot extend the match, so a naive trim stripped a legal picture symbol and broke the golden.
   With the guard: `77 X PIC 99, VALUE 3.` now lexes the picture as "99" (WAS the silent numeric-edited "99,"
   misbind on CONFORMING source — the W2 review's serious finding, the `;` bug's twin), and NC125A stays
   byte-exact. The Analyze single-`;`-strip stays as defense-in-depth.
3. **XOR/EXCLUSIVE-OR regated to 2023 (VCR rows 32/41; the chair adjudication executed).** The operator
   alternative is `{is2023()}?`-gated (CobolExpressions.g4); both tokens joined `cobolWord` +
   `_dataNameTokens` + the funnel's `CheckedTokenTypes` (they are high-confidence 2023 rows in the §8.9
   table — my earlier "missing from the table" read was a bad probe). Verified end-to-end: the operator runs
   at 2023 (`T`), rejects below with the W1.5 0900 hint naming COBOL-2023 (a new `EditionGateHints` entry —
   the rule-stack filter had to go: the condition rule pops before the report, and a parse error AT an
   XOR token below 2023 is the gated operator by construction); `01 XOR PIC 9.` compiles at 85/2002/2014
   (the continuity invariant), rejects 0901×2 at 2023 strict, warns + runs at 2023 permissive. THREE new
   registry+json rows (`logical-xor-operator-2023`, `user-word-xor-2023`, `user-word-exclusive-or-2023`);
   `tests/conformance/2002/logical_xor` re-editioned to `2023/`; the ISO2023_CONFORMANCE_PLAN M4-2a
   "(2002-era)" parenthetical corrected (it was unsourced and wrong — Annex E.2 item 25 / E.3.2 item 4).
   The legacy `SpecFixTests.LogicalXor_TruthValuesAndPrecedence` targeted Cobol2002 (the mislabel encoded in
   a test) — retargeted to Cobol2023 with the citation.

**Battery: build 0W/0E (fresh regen) · unit 102/102 · conformance 1367/1367 (+14) · sweep 419 OK / 0 BREAKS ·
INV-1-STRONG 349/349 byte-exact at 2023-permissive · FULL legacy guard: NIST 353 MATCH / 0 regressions,
legacy unit 1204/1204, integration 537 green (the one XOR-test fix above).**

