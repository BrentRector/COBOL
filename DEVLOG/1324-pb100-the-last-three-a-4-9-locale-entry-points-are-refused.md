## Entry 1324 — 2026-08-18 16:08 PDT — PB100: the last three A.4.9 locale entry points are refused BY NAME — the ALPHABET clause's LOCALE phrase, PICTURE format 2, the EC-LOCALE / EC-ORDER-NOT-SUPPORTED exception-names — and CONFORMANCE.md §4 item 5's "every entry point" is true again

**What landed.** kb/Work **PB100** (MINOR; the posture-repair half — T0 — of the PB64 locale design draft, owed
under EITHER answer to its owner-reserved Q1): COBOL.NET's ratified posture for the optional locale module is
documented non-support (council decision 3, 2026-07-03), which Annex A.4.1 makes conforming ONLY when each element
is refused by name — and CONFORMANCE.md §4 item 5 already CLAIMED that for "every locale entry point the module
names". Three were not: `ALPHABET A1 IS LOCALE` drew a FALSE COBOLNET0901 ("'LOCALE' is a reserved word … cannot be
used as a user-defined word" — about a program using it as nothing of the sort; the FOR NATIONAL branch added a
"not a supported code-name" 0898), `01 X PIC +$9.9 LOCALE SIZE IS 10` was a raw parse error at SIZE, and the
EC-LOCALE family / EC-ORDER-NOT-SUPPORTED were ACCEPTED in USE / RAISE / RAISING / WHEN / >>TURN — the syntax of an
unclaimed optional element, accepted (A.4.9 item 1). PB78 (CHARACTER CLASSIFICATION, the computer-name) and PB92
(SET LOCALE formats 11/12) had landed the draft's other posture rows; this closes T0.

**As built.** (a) `DataBinder.AlphabetBind` recognizes `IS LOCALE [locale-name-2]` in either branch at 2002+
through `IsAlphabetLocalePhrase` (the ONE shape test the §8.9 funnel's exemption shares) → COBOLNET1518, and
registers an identity alphabet so a PROGRAM COLLATING SEQUENCE reference binds without a cascade; (b) the grammar's
`pictureClause : PIC PIC_STRING editingPhrase* pictureLocalePhrase?` with the text predicate `pictureLocaleAhead()`
(LOCALE is not a lexer token — a plain word at COBOL-85, reserved 2002+) → COBOLNET1518 in the entry binder, the
character-string analyzed as Format 1 for recovery; (c) `EcNameResolution.TryResolve` — the ONE exception-name
funnel (kb/Work R05) — refuses the EC-LOCALE family and EC-ORDER-NOT-SUPPORTED with COBOLNET1518, so USE, RAISE,
EXIT / GOBACK RAISING, the exception-checking PERFORM's WHEN and >>TURN all refuse them (the PROCEDURE DIVISION
header's RAISING admits only EC-USER level-3 names, §14.2.2 SR7, so it never reaches them); the §8.9 funnel exempts
the two keyword slots (`AlphabetEntryContext` under a LOCALE-phrase definition, `PictureLocalePhraseContext`) — the
PB27 / PB78 / PB92 shape. Negatives `pb100-alphabet-locale-phrase`, `pb100-picture-locale-format-2`,
`pb100-ec-locale-names`; CONFORMANCE.md §4 item 5 names all three; SR-12.3.7.3-24 / GR-12.3.7.4-5 →
DOCUMENTED-NON-SUPPORT (GAP 3935 → 3933).

**Battery #19** (PB99, tree `a6034ca8`, recorded in §0 with `b271b936`): Conformance 4793/4793 · Unit 4239/4239 ·
Characterization 33/33 · NIST 353/0 audit-clean · differential 1323 cases, 0 flips. ALL GREEN.

**Gate (PB100, wave-local).** Characterization 33/33; unit (Ec / Turn / alphabet / reserved-word / picture / locale / grammar / lexer / SPECIAL-NAMES) 3103/3103; conformance (the corpus runner + manifests, SPECIAL-NAMES / alphabet / locale / exception / EC / reserved-word / picture / version-matrix / collating) 3412/3412 (10 m 47 s). Battery #20 rides with the next landing.

**Where this leaves the register.** `work.py next` lists ONLY PB64 — the locale FEATURE, whose Q1 (reverse the
ratified documented-non-support decision and implement the A.4.9 module?) and Q2–Q4 are the owner's. The next
autonomous work is Phase-B adjudication; a new defect found on the way outranks it.

