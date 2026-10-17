      *>--- conformance-case
      *> id: pb362_declaratives_reference_boundary
      *> rules: [SR-14.9.49.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.3 SR4 (kb/Work PB362): "Procedure-names within a
      *> declarative section may be referenced in a different declarative
      *> section or in a nondeclarative procedure only with a PERFORM
      *> statement." The LEGAL half of the boundary, executed:
      *>   1. PERFORM DP1 THRU DP2 from the nondeclarative portion. DP1's
      *>      GO TO DP2 is a reference WITHIN its own section, which SR4
      *>      does not restrict. §14.9.28.4 runs DP2's ADD, then control
      *>      returns at the end of DP2: N = 1.
      *>   2. PERFORM D1 (the section): DP1, GO TO DP2, ADD: N = 2.
      *>   3. PERFORM DP3, a paragraph of declarative section D2, whose
      *>      PERFORM DP2 references a DIFFERENT declarative section by
      *>      PERFORM, the one statement SR4 admits: N = 3.
      *> The refused half (a GO TO into a declarative section from outside
      *> it) is conformance:cases/14.9.49-USE/pb362-goto-into-declarative.
      *> EDITION: SR4 is COBOL-85's USE syntax rule, unchanged in 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB362DRB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb362drb-f1.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       DP1.
           GO TO DP2.
       DP2.
           ADD 1 TO N.
       D2 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       DP3.
           PERFORM DP2.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           PERFORM DP1 THRU DP2.
           DISPLAY "AFTER-RANGE N=" N.
           PERFORM D1.
           DISPLAY "AFTER-SECTION N=" N.
           PERFORM DP3.
           DISPLAY "AFTER-CROSS N=" N.
           STOP RUN.
