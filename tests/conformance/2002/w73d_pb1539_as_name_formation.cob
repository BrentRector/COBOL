       *> kb/Work PB1539 - an externalized name is FORMED by one rule on both sides.
       *> ISO 8.3.2.2 2): "the content of the literal specified in that AS phrase is
       *> a name that is externalized to the operating environment. The implementor
       *> defines the formation and mapping rules of these names."  DOC-A.1-68 fixes
       *> formation: leading and trailing spaces are removed, from an AS literal and
       *> from a CALL target alike; mapping ignores case.  ISO 14.9.4.4 GR3 b) makes a
       *> CALL literal "the program-name of the program being called, as described in
       *> 8.3.2.2", so all three CALLs below name PB1539S, and 8.3.2.2 ("when two or
       *> more source elements identify something with the same externalized name,
       *> they refer to the same instance") makes the two EXTERNAL items one item.
       *> Each AS literal with spaces draws warning COBOLNET2642.
       *> Expected (derived from those rules, not observed):
       *>   IN PB1539S 1   - CALL "pb1539-trail" (the formed name, other case)
       *>   IN PB1539S 2   - CALL " PB1539-TRAIL  " (formed the same way)
       *>   IN PB1539S 3   - CALL identifier holding "  pb1539-trail" (run-time form)
       *>   SHARED=ABC     - EXTERNAL AS "PB1539 Shared " is EXTERNAL AS "pb1539 shared"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1539M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-TARGET PIC X(20) VALUE "  pb1539-trail".
       01  WS-SHARED PIC X(3) EXTERNAL AS "PB1539 Shared ".
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABC" TO WS-SHARED.
           CALL "pb1539-trail".
           CALL " PB1539-TRAIL  ".
           CALL WS-TARGET.
           STOP RUN.
       END PROGRAM PB1539M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1539S AS " PB1539-Trail  ".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-COUNT PIC 9 VALUE 0.
       01  WS-VIEW PIC X(3) EXTERNAL AS "pb1539 shared".
       PROCEDURE DIVISION.
       S-P.
           ADD 1 TO WS-COUNT.
           DISPLAY "IN PB1539S " WS-COUNT.
           IF WS-COUNT = 3
               DISPLAY "SHARED=" WS-VIEW
           END-IF.
           GOBACK.
       END PROGRAM PB1539S.
