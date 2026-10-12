## Entry 237 — File-I/O wall, part 1: spec-conformant FILE-CONTROL / READ / USE forms now parse

Starting the file-I/O FILE-CONTROL wall (~144/162 SQ/IX/RL were COMPILE_FAIL). The prior session
flagged the CCVS FILE-CONTROL forms as "non-standard" and warned against relaxing the grammar — but
checking each against ISO/IEC 1989:2023 shows they ARE conformant; the grammar was simply too strict.
So these are spec FIXES, not relaxations (the `<u>STATUS</u>` underlining in §12.4.5.8.2 and the
bracketed `[ORGANIZATION IS]` in §12.4.5.10 are the decisive evidence; §12.4.5.2 SR1 makes the
clauses order-free; §6/8 the optional-word rule, "uppercase words that are not underlined are
optional words … with no effect on the semantics").

Grammar fixes (`CobolIO.g4`, `CobolControlFlow.g4`; parser auto-regenerated):
- **FILE-CONTROL clauses are order-free** (ISO §12.4.5.2 SR1: "The clauses that follow the SELECT
  clause may appear in any order"). `ASSIGN` moved out of its fixed pre-clause slot into the
  order-free `fileControlClauses*`; `SemanticBuilder` reads it from the clause loop. Fixes the
  `ACCESS MODE … ASSIGN … ORGANIZATION` ordering (SQ104A).
- **`[ORGANIZATION IS]` is optional** (ISO §12.4.5.10): `organizationClause : (ORGANIZATION IS?)?
  organizationType` — a lone `SEQUENTIAL` is a valid ORGANIZATION clause (SQ102A).
- **`[FILE] STATUS [IS]` — only STATUS is required** (ISO §12.4.5.8.2, only STATUS underlined):
  `fileStatusClause : FILE? STATUS IS? dataReference` — accepts bare `STATUS data-name` (SQ105A).
  (This is the form the old, overly-strict COBOL0200 hint warned against.)
- **`[AT] END` — AT is an optional word** in the at-end phrase: `readAtEnd : AT? END …`. Fixes
  `READ … RECORD END …` (the dominant SQ blocker — most SQ tests).
- **USE `[STANDARD]` and `[ON]` optional**: `USE GLOBAL? AFTER STANDARD? (EXCEPTION|ERROR) PROCEDURE
  ON? useOnTarget` — accepts both `USE GLOBAL AFTER ERROR PROCEDURE ON INPUT` (IC233A) and
  `USE AFTER STANDARD ERROR PROCEDURE OUTPUT` (SQ105A). (Code-block formats lose underlining; these
  follow the optional-word rule + universal CCVS/compiler practice — documented as such.)

Result: SQ compile count **2 → 26** (of 85). The remaining SQ COMPILE_FAILs are now mostly a SEMANTIC
check — `CBL3203: FILE STATUS cannot be group item` (~40 tests) — to investigate next, plus a few
more parse forms (LINAGE-COUNTER, RECORD … CHARACTERS, RECORD DELIMITER). Full guard ALL GREEN
(1000 / 348 / 152) — no regression from the broad grammar change.

