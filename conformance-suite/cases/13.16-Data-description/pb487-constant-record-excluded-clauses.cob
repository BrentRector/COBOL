*>--- conformance-case
*> id: pb487-constant-record-excluded-clauses
*> rules: [SR-13.16.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - ISO 13.16.3 syntax rule 13, paragraph 1: "The ANY LENGTH, BASED, BLANK WHEN ZERO,
      *> DYNAMIC LENGTH, select-when, SYNCHRONIZED, and TYPEDEF clauses and validation-clauses shall not be
      *> specified in the same data description entry with the CONSTANT RECORD clause, or in any data
      *> description entry subordinate to a data description entry with the CONSTANT RECORD clause."
      *> EIGHT excluded clauses.  The check was an `||` chain over five decode flags - ANY LENGTH, BASED,
      *> BLANK WHEN ZERO, SYNCHRONIZED, TYPEDEF - so DYNAMIC LENGTH, select-when and the validation clauses
      *> were invisible to it and this entry compiled clean.  The rule now reads the WRITTEN clause SET of the
      *> entry against ConstantRecordExcluded, a constant transcribed from the sentence above and pinned by
      *> DataClauseKindDriftTests, and the message NAMES which excluded clause was specified.
      *> COBOLNET1549 (the CONSTANT RECORD band).  COBOLNET1563 fires too and correctly: SR18 is the mirror
      *> rule, and CONSTANT RECORD is not among DYNAMIC LENGTH's five permitted co-clauses either.
      *> Rejected from 2014, not 85/2002: DYNAMIC LENGTH is a COBOL-2014 introduction, so below it this source
      *> is a different program and the introduction gate answers first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487CR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A CONSTANT RECORD DYNAMIC LENGTH PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
