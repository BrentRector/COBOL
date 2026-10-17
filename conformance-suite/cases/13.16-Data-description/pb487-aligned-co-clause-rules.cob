*>--- conformance-case
*> id: pb487-aligned-co-clause-rules
*> rules: [SR-13.16.3-12, SR-13.16.3-18, SR-13.18.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB487 - the 13.16.3 PERMITTED-SET rules, which were unenforceable while ALIGNED had no grammar
      *> rule and the vendor catch-all swallowed the word.
      *> SR18: "If a DYNAMIC LENGTH clause is specified, the only other clauses permitted are level-number,
      *> entry-name, PICTURE, USAGE, and VALUE."  ALIGNED is not among them, so entry A violates SR18
      *> (COBOLNET1563) and, separately, 13.18.1.3 SR1 (COBOLNET1942 - an alphanumeric elementary item is
      *> neither a bit group item nor an elementary bit data item).
      *> SR12: "The SAME AS clause shall not be specified in the same data description entry with any clauses
      *> except CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL, level-number, and OCCURS."  ALIGNED is not among
      *> them either, so entry C violates SR12 (COBOLNET1555) and SR1 again.
      *> Both rules were `||` chains over whichever decode flags the author remembered - SR12's could not see
      *> ALIGNED, DYNAMIC LENGTH, GROUP-USAGE, PROPERTY, SELECT WHEN or the validation clauses, and SR18's was
      *> short of four more.  They now read ONE clause-presence SET taken from the parse tree, with
      *> DataClauseKindDriftTests asserting every dataDescriptionClause alternative is in it.
      *> Rejected from 2014, not 85/2002: DYNAMIC LENGTH is a COBOL-2014 introduction, so below it this source
      *> is a different program and the introduction gate answers first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487OC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC X(3).
       01 A DYNAMIC LENGTH PIC X ALIGNED.
       01 C SAME AS Z ALIGNED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
