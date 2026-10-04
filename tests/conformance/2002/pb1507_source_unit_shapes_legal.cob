      *> kb/Work PB1507 - THE LEGAL SPELLINGS NEXT TO THE 10.6.1 SHAPE RULES (negatives pb1507-*).
      *>   10.6.1: a function-definition is `FUNCTION-ID. ... [ procedure-division ] END FUNCTION name.` - the end marker
      *>     unbracketed, no contained slot - and it is a source unit of the compilation group, ahead of the program that
      *>     uses it; a program-definition is `[ procedure-division [ program-definition ] ... ] [ END PROGRAM name. ]`,
      *>     so MID1507 (a procedure division) contains LEAF1507, which is contained in a program that is itself
      *>     contained.
      *>   11.10.2 Format 1 + 5.2.6.4: the attribute group `IS { COMMON | { INITIAL | RECURSIVE } } PROGRAM` takes any
      *>     order, each alternative once: LEAF1507 is `IS INITIAL COMMON PROGRAM` - COMMON is allowed because it is
      *>     contained (11.10.3 SR4), INITIAL because no container is recursive (SR5).
      *>   Derived output: F=7 (the function); MID's working-storage is last-used (MID 1, MID 2) while the INITIAL
      *>     LEAF1507 starts from its VALUE on every activation (LEAF 1 twice).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F1507.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 7 TO R
           GOBACK.
       END FUNCTION F1507.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1507OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F1507.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
           COMPUTE X = FUNCTION F1507
           DISPLAY "F=" X
           CALL "MID1507"
           CALL "MID1507"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MID1507.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO M
           DISPLAY "MID " M
           CALL "LEAF1507"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LEAF1507 IS INITIAL COMMON PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO L
           DISPLAY "LEAF " L
           GOBACK.
       END PROGRAM LEAF1507.
       END PROGRAM MID1507.
       END PROGRAM PB1507OK.
