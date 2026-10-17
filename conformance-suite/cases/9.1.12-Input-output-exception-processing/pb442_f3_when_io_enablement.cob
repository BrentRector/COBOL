*>--- conformance-case
*> id: pb442_f3_when_io_enablement
*> rules: [GR-9.1.12-1, GR-14.9.28.4-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB442 - an I-O statement in a paragraph PERFORMed from an exception-checking PERFORM's
      *> imperative-statement-1: the WHEN phrase selects its condition exactly when the condition is
      *> RAISED there, and enablement is a property of the paragraph's own text (docs/CONFORMANCE.md §3
      *> D-F3S (c)).
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  9.1.12 - "When execution of an input-output statement raises an exception condition", item 1
      *>    selects a WHEN phrase "of an active PERFORM statement" before item 2's USE declaratives.
      *>  9.1.13.1 - "Whether or not the exception condition is raised depends on whether or not checking
      *>    for that exception condition is enabled"; 7.3.25.4 GR6 - a TURN enables checking for the
      *>    statements "that follow in the compilation group"; 14.9.28.4 GR14's implicit TURN is written
      *>    before imperative-statement-1's first statement, so it covers the PERFORM's own text only.
      *>  Q1: RD-A precedes the >>TURN below, so its READ past end of file raises nothing; the at-end
      *>    status 10 is an unsuccessful completion, and 14.9.49.4 GR6 runs the USE procedure for F (no
      *>    exception condition, so none matched a WHEN): DECL-F 10, then RD-A continues.
      *>  Q2: RD-B follows the >>TURN, so its READ raises EC-I-O-AT-END while the PERFORM is active; the
      *>    WHEN EXCEPTION F phrase selects it and the USE declarative is ignored (14.9.28.4 GR17), and the
      *>    nonfatal condition continues after the READ (GR20): Q2-WHEN, then RD-B continues.
      *>
      *> WHY EACH LEG CAN FAIL: Q1 fails if the PERFORM's implicit TURN leaked into text outside it; Q2
      *> fails if the WHEN reached only statements written inside the PERFORM.
      *>
      *> EDITIONS: 2023 only - the exception-checking PERFORM is a 2023 facility; the edition gate is
      *> negative/pb441-f3-perform-below-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442IOEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb442ioen.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-F SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       D-F-P.
           DISPLAY "DECL-F " F-ST.
       END DECLARATIVES.
       MAIN SECTION.
       M-SEED.
           OPEN OUTPUT F
           CLOSE F.
       M-Q1.
           OPEN INPUT F
           PERFORM
               PERFORM RD-A
               DISPLAY "Q1-AFTER"
           WHEN EXCEPTION F
               DISPLAY "Q1-WHEN " F-ST
           END-PERFORM
           CLOSE F.
       M-Q2.
           OPEN INPUT F
           PERFORM
               PERFORM RD-B
               DISPLAY "Q2-AFTER"
           WHEN EXCEPTION F
               DISPLAY "Q2-WHEN " F-ST
           END-PERFORM
           CLOSE F
           STOP RUN.
       RD-A.
           READ F
           DISPLAY "RD-A-AFTER".
       >>TURN EC-I-O CHECKING ON
       RD-B.
           READ F
           DISPLAY "RD-B-AFTER".
