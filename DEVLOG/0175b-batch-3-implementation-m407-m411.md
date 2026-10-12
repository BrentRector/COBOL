## Entry 175 — 2026-03-31: Batch 3 Implementation — M407 + M411

**M407 (CURRENCY SIGN WITH PICTURE SYMBOL)**: Implemented the PICMODE exploit design.
`WITH PICTURE SYMBOL` triggers the lexer's PICMODE, which captures "SYMBOL" as PIC_STRING.
Parser sees `WITH PIC PIC_STRING literal`. SemanticBuilder validates PIC_STRING == "SYMBOL",
extracts literal-7 as CurrencyOutputChar and literal-8 as CurrencySign. PicEnvironment
gained CurrencyOutputChar field. PicRuntime output placements (3 sites) changed to use
CurrencyOutputChar. De-editing paths (3 sites) changed to strip CurrencyOutputChar instead
of hardcoded "$". CIL emission updated for 3-param PicEnvironment ctor. Diagnostics for
invalid literal-7/literal-8 characters. 6 integration tests: fixed/floating custom currency,
BLANK WHEN ZERO, explicit dollar, decoupled symbol/output.

**M411 (SCREEN SECTION)**: Grammar island implemented. 17 new lexer tokens (SCREEN, COL,
COLUMN, BELL, BLINK, HIGHLIGHT, LOWLIGHT, REVERSE-VIDEO, UNDERLINE, FOREGROUND-COLOR,
BACKGROUND-COLOR, SECURE, AUTO, FULL, ERASE, REQUIRED, EOL, EOS). New CobolScreen.g4
parser fragment with screenSection, screenDescriptionEntry, and ~15 clause rules. Reuses
existing pictureClause, valueClause, blankWhenZeroClause, etc. BoundScreenItem class with
all fields (position, attributes, data binding, hierarchy). SemanticBuilder.VisitScreenSection
builds screen item tree with level-number stack pattern, validates mutual exclusivity
(HIGHLIGHT vs LOWLIGHT, USING vs FROM+TO). SemanticModel.ScreenItems property. 12 integration
tests: empty section, VALUE+LINE+COL, PIC+USING, PIC+FROM+TO, all attributes, colors,
LINE PLUS/COL PLUS, BLANK SCREEN, ERASE EOL/EOS, SECURE+REQUIRED, nested levels, AUTO+FULL.

All screen-related tokens added to cobolWord for keyword shadowing mitigation. LPAREN
trigger refactored to use HashSet whitelist (see Entry 176).

Regression: 922 unit + 329 integration + 95 NIST = 0 failures.

---



ANTLR grammar review agent found 14 warnings and 6 notes. All addressed:

**cobolWord propagation (W1-W14)**: 17 edits across 6 grammar files — every remaining bare
IDENTIFIER in name-reference positions changed to cobolWord. Affected rules: assignTarget,
classDefinitionClause, alphabetClause, alphabetEntry, implementorSwitchEntry, switchOnClause,
switchOffClause, sortDuplicatesPhrase, sortCollatingPhrase, programCollatingSequenceClause,
reportName, reportGroupName, dataReferenceAttribute, codeSetClause, labelRecordsClause,
dataRecordsClause, className, displayUpon.

**Dialect gates (N1-N3)**: Added `{is2002()}?` predicates to STOP STATUS, START WITH LENGTH,
and FOR ALPHANUMERIC/NATIONAL on CLASS/ALPHABET/SYMBOLIC CHARACTERS. These are COBOL-2002+
features, not COBOL-85. Default dialect remains 85.

**Dialect wiring**: Made DialectLevel public on CobolParserCoreBase. Wired Options.Dialect
through Compilation.LexAndParse to parser.DialectLevel. Added dialect parameter to
EndToEndTestBase.CompileAndRun. COBOL-2002+ tests now set DialectMode.Cobol2002 explicitly.

**New ledger items**: M428 (semantic validation for SYMBOLIC CHARACTERS N:N count equality).

Regression: 922 unit + 311 integration + 95 NIST = 0 failures.

---

