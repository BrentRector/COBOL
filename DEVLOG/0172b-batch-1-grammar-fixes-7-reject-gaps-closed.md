## Entry 172 — 2026-03-30: Batch 1 Grammar Fixes — 7 REJECT gaps closed

Implemented 7 COBOL-85 grammar gaps that were rejecting valid programs:

**Grammar changes (4 files):**
- CobolControlFlow.g4: USE statement expanded to support GLOBAL keyword, EXCEPTION/ERROR
  synonym, INPUT/OUTPUT/I-O/EXTEND mode targets. STOP RUN expanded with WITH ERROR/NORMAL
  STATUS phrase.
- CobolIO.g4: START statement expanded with WITH LENGTH phrase for partial-key matching.
- CobolSpecialNames.g4: ALPHABET and CLASS clauses expanded with FOR ALPHANUMERIC/NATIONAL.
- CobolLexer.g4: Added LENGTH, NATIONAL, NORMAL as dedicated lexer tokens. Added all three
  to LPAREN subscript-mode trigger list.

**Architectural refactor — `cobolWord` rule:**
Production-quality refactor: created centralized `cobolWord` parser rule (IDENTIFIER | LENGTH
| NATIONAL | NORMAL) and propagated it through dataReference, qualification, programName,
procedureName, fileName, computerName, dataName. Eliminated per-rule token hacks. All C# call
sites updated: `.IDENTIFIER()` → `.cobolWord()` across 24 files.

**Binder updates:**
- BoundUseStatement expanded with IsGlobal (bool) and TargetMode (OpenMode?) properties.
- FileIoBinder.BindUse() updated for new useOnTarget grammar structure.
- functionName rule expanded with LENGTH and NATIONAL alternatives.

**Tests added:** 15 new integration tests in GrammarBatch1Tests.cs covering all 7 constructs
plus cobolWord data-name regression tests (LENGTH and NORMAL as data names).

**Regression:** 922 unit + 302 integration + 95 NIST = 0 failures. 15 net new integration tests.

---

