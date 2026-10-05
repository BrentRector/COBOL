      *> kb/Work PB1421 — GO TO Format 2's DEPENDING ON identifier-1, and its sibling WRITE ... ADVANCING
      *> identifier-2, are IDENTIFIER positions, so each admits a function-identifier (ISO/IEC 1989:2023
      *> §8.4.3.1.2 Format 1) the way the PERFORM ... TIMES count does (kb/Work PB86). Both were a parse error.
      *>   §8.4.3.2.4 GR1  "A function-identifier references a temporary data item whose value is determined
      *>                   when the function is referenced at runtime" — for an INTEGER function an elementary
      *>                   numeric integer item, which is exactly what each position's syntax rule requires:
      *>   §14.9.17.3 SR1  "Identifier-1 shall reference a numeric elementary data item that is an integer."
      *>   §14.9.51.3 SR14 "Identifier-2 shall reference an integer data item."
      *>   (§8.4.3.2.3 SR11 keeps a NUMERIC function out of both: negative pb1421-goto-depending-numeric-function.)
      *> EXPECTED OUTPUT, derived from §14.9.17.4 GR2 ("control is transferred to procedure-name-1, etc.,
      *> depending on the value of identifier-1 being 1, 2, ... , n"; any other value: no transfer):
      *>   A  FUNCTION INTEGER(N), N = 2.5: "The INTEGER function returns the greatest integer value that
      *>      is less than or equal to the argument" (§15.44.1) = 2 -> the second target    -> A P2
      *>   B  FUNCTION LENGTH(X), X PIC X(3): 3 -> the third target                          -> B Q3
      *>   C  FUNCTION LENGTH("ABCDE") = 5 is outside 1..3 (n = 3): no transfer, and the next
      *>      statement in the normal sequence runs                                         -> C FELL THROUGH
      *>   D  WRITE ... AFTER ADVANCING FUNCTION INTEGER(M) LINES (M = 1.5, so 1 line) compiles and
      *>      runs (it was a parse error)                                                   -> D WRITTEN
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1421GD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "pb1421gd.txt"
               ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD WF.
       01 WR PIC X(4).
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "ABC".
       01 N PIC 9V9 VALUE 2.5.
       01 M PIC 9V9 VALUE 1.5.
       PROCEDURE DIVISION.
       STEP-A.
           GO TO A1 A2 A3 DEPENDING ON FUNCTION INTEGER(N).
           DISPLAY "A FELL THROUGH".
           GO TO STEP-B.
       A1.
           DISPLAY "A P1".
           GO TO STEP-B.
       A2.
           DISPLAY "A P2".
           GO TO STEP-B.
       A3.
           DISPLAY "A P3".
       STEP-B.
           GO TO Q1 Q2 Q3 DEPENDING ON FUNCTION LENGTH(X).
           DISPLAY "B FELL THROUGH".
           GO TO STEP-C.
       Q1.
           DISPLAY "B Q1".
           GO TO STEP-C.
       Q2.
           DISPLAY "B Q2".
           GO TO STEP-C.
       Q3.
           DISPLAY "B Q3".
       STEP-C.
           GO TO C1 C2 C3 DEPENDING ON FUNCTION LENGTH("ABCDE").
           DISPLAY "C FELL THROUGH".
           GO TO STEP-D.
       C1.
           DISPLAY "C C1".
           GO TO STEP-D.
       C2.
           DISPLAY "C C2".
           GO TO STEP-D.
       C3.
           DISPLAY "C C3".
       STEP-D.
           OPEN OUTPUT WF.
           MOVE "LINE" TO WR.
           WRITE WR AFTER ADVANCING FUNCTION INTEGER(M) LINES.
           CLOSE WF.
           DISPLAY "D WRITTEN".
           STOP RUN.
