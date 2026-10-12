## Entry 1673 — 2026-09-24 10:11 PDT — Registrar 15a: batch 15a recorded (28 text-manipulation / compiler-directive subjects), 35 notes filed (PB1350–PB1384), GAP 1845 → 1812

**What.** Registrar 15a registered lane-3 adjudication batch 15a: the §7.2 text-manipulation and §7.3 compiler-directive
subjects (COPY, REPLACE, text-words, compile-time arithmetic/boolean expressions, the constant conditional expression,
the defined condition, and the CALL-CONVENTION, COBOL-WORDS, DEFINE, DISPLAY, EVALUATE, FLAG-02, FLAG-14, IF,
LEAP-SECOND, LISTING, PAGE, POP, PROPAGATE, PUSH, REF-MOD-ZERO-LENGTH, SOURCE and TURN directives), adjudicated on the
pinned tree `adj-d2` @071ff04ea. Nothing under `src/` landed between the pin and the registrar's base, so no finding
became a report lead. **GAP 1845 → 1812** (+33 closed of 4,348).

**Verdicts.** 233 records: DIVERGES 98 · CONFORMS 80 (33 closed their row, 47 are CONFORMS-but-untested and go to the
golden lane's next input set) · PARTIAL 53 · NOT-IMPLEMENTED 2. No NEEDS-OWNER-DECISION rows, no owner-verdict rows.
The refuters re-examined 112 CONFORMS rows and overturned 36: 31 to PARTIAL, 1 to DIVERGES, and 4 upheld as CONFORMS
with the test-ref withdrawn. Every overturn went toward MORE defect or LESS evidence. Two recurring refuter shapes: the
adjudicator's 'caveat' was the defect (the lazy `>>WHEN` parse, SR-7.3.13.3-3..6), and a rule verified on the ordinary
operand shape broke on a legal edge one (a literal containing `==`, a prefixed literal, a RENAMES alias).

**Batch mechanics (friction, logged).** One refuter checkpoint (`refute-compile-time-boolean-expressions.jsonl`) was
written in cp1252 (a raw `§` byte) and crashed `merge_batch.py`; the registrar's COPY was re-encoded to UTF-8 (the
original left untouched). 220 records carried free-text `editions` and 29 carried prose in `code-location`; both were
normalized mechanically with the prose moved into `notes` (nothing dropped), and 138 `Type.Member` code-locations were
reduced to the member the `EveryCodeLocation_ResolvesInTheTree` gate can find (3 unresolvable items moved to notes).

**Notes: 35 filed, one per mechanism, clustered into fill units by code site.** The ones that change what a user's
program does:

- **Wrong answers (MAJOR):**
  - PB1350: the text-word tokenizer (`CopyProcessor.TokenizeTextWords`, shared by REPLACE and COPY REPLACING) forms
    the wrong §7.2.2.5 text-words — no colon separator, comma/semicolon separators 'not followed by a digit' instead
    of 'followed by a space', prefixed literals split, doubled quotes split — so replacements match inside literals and
    picture strings and miss across `A:B`.
  - PB1351: literal text-words are compared case-insensitively on their raw spelling (content case-folded; quote vs
    apostrophe never match; doubled quotes not collapsed).
  - PB1352: COPY REPLACING rewrites the copybook's own `>>IF`/`>>DEFINE` lines (it runs before the CC driver) and
    REPLACE rewrites `>>TURN`/`>>PUSH`/…, so the wrong branch compiles.
  - PB1354: `COPY t SUPPRESS REPLACING …` (the general-format order) silently LOSES the REPLACING phrase — the COPY
    header is a forgiving hand scan, not a parse.
  - PB1355: a missing copybook becomes a COMMENT with no diagnostic (`strict: false` always), and library-name falls
    back to the default library.
  - PB1361: a fixed-form directive with a non-numeric sequence area is not recognized, so the rest of the program is
    read in the wrong reference format and silently prints nothing.
  - PB1362: the main source's reference format is auto-detected instead of defaulting to fixed.
  - PB1363: the conditional-compilation frame stack never validates structure (a second `>>ELSE` re-opens emission).
  - PB1370: compile-time boolean expressions (parenthesized literal rejected, shift count unscreened, B-SHIFT ungated
    below 2023, and the SHARED resolver's B-NOT/shift precedence).
  - PB1379: a tiny negative FORMATTED-TIME/-DATETIME/LOCALE-TIME-FROM-SECONDS float argument is truncated to 0 before
    the standard-numeric-time-form screen and formatted as midnight.
  - PB1380: a reference-modified RENAMES alias drops its reference modifier (`RN(2:3)` moves six characters).
  - PB1382: a successful READ with status 04/06/09 never raises EC-I-O-WARNING under TURN.
- **Rejects legal source (MAJOR):** PB1353 (`=="A==B"==`), PB1357 (REPLACE ALSO / LAST), PB1358 (REPLACE mid-line),
  PB1359 (the floating literal continuation indicator), PB1365 (`>>TURN … CHECKING` with implied ON), PB1368
  (CONSTANT FROM a compilation variable), PB1371 (XOR in a constant conditional expression), PB1372/PB1373 (COBOL-WORDS
  lexer seam and operand screens), PB1374 (FLAG-02/FLAG-14 default spellings), PB1378 (LEAP-SECOND between two units).
- **Under-rejects / silent (MINOR):** PB1356, PB1360, PB1364, PB1366, PB1367, PB1375, PB1376, PB1377, PB1381,
  PB1383, PB1384; PB1369 is the §7 Annex A.1 documentation (process-only).

**Extended, not duplicated:** PB1067 (library-text reference format, GR3/GR5, GR-7.3.4-5), PB806 (ELSE/END-IF/
END-EVALUATE trailing words own four more syntax rules), PB807 (>>DISPLAY operand: SR2/SR3 and §7.3.3 SR10),
PB1119 (PROPAGATE inert: the directive's own GR1-GR3), PB1229 (14d; the figurative ZERO in a compile-time arithmetic
expression also breaks §7.3.6.2 SR1), PB1065 and PB963 (already owned their rows). PB372 received the batch's dossier
gaps: most are its class — implementing code that cites no clause (COPY has no grammar rule; `TokenizeTextWords` cites a
nonexistent '§7.3.2'). PB1385–PB1389 unused.

**Gate.** Whole Unit assembly: Passed 29190 / Failed 0 (the GnuCOBOL corpus was fetched into the worktree first, so the two ExternalCorpusPopulationDriftTests ran green rather than absent). `work.py check`: all well-formed.

