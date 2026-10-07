      *> kb/Work PB397 - the three syntax rules stated over a statement SEQUENCE, in every LEGAL position they leave
      *> open. They are present unchanged at all four editions (the three rules carry no edition qualifier in ISO/IEC 1989:2023), so this
      *> is the single positive golden at the earliest edition; the rejected positions are the negatives
      *> pb397-exit-not-alone, pb397-go-to-not-last and pb397-stop-not-last, which name all four editions.
      *>
      *> 14.9.14.3 SR1: "The EXIT statement shall appear in a sentence by itself that shall be the only sentence
      *>   in the paragraph or in a section without paragraphs." EX-PARA below is that paragraph, and EXIT has no
      *>   effect (14.9.14.4 GR1), so PERFORM EX-PARA prints nothing and returns.
      *> 14.9.17.3 SR2: "If a GO TO statement represented by format 1 appears in a consecutive sequence of
      *>   imperative statements within a sentence, it shall appear as the last statement in that sequence."
      *>   Legal: last of its sentence (MAIN-P); the whole THEN phrase of an IF with statements written AFTER the
      *>   END-IF (SEQ-A); last of the ELSE phrase (SEQ-B). A Format 2 GO TO is not constrained by SR2, and when its
      *>   selector is not 1..n control "passes to the next statement in the normal sequence" (14.9.17.4 GR2), so
      *>   the statements written after it run (SEQ-C).
      *> 14.9.42.3 SR1: "The STOP statement shall be specified only as the last statement in any discreet block of
      *>   code." Read as the consecutive sequence (docs/CONFORMANCE.md D-SEQ): last of a THEN phrase followed by
      *>   an ELSE phrase, and last of a THEN phrase followed by statements after the END-IF (SEQ-E). The
      *>   X3.23-1985 STOP literal is not that statement: it communicates the literal to the operator and
      *>   execution continues with the next statement, so a statement after it runs (SEQ-E,
      *>   AFTER-STOP-LITERAL; the literal goes to the operator channel, stderr, not to this output).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN (N = 1, K starts at 2):
      *>   MAIN-P    prints M1, GO TO SEQ-A.
      *>   SEQ-A     N = 1: the GO TO leaves; "A-MUST-NOT" would print only if it were ignored.
      *>   SEQ-B     N is not 2: the ELSE phrase prints B-ELSE and leaves for SEQ-C.
      *>   SEQ-C     K = 0 selects nothing (GR2), so C-FALL prints; K = 2 selects the second name, SEQ-E.
      *>   SEQ-E     prints E, performs EX-PARA, prints AFTER-EXIT, passes the STOP literal and prints
      *>             AFTER-STOP-LITERAL; the first IF is false (N is not 2) and its
      *>             ELSE prints ELSE-BRANCH; the second IF is true, prints STOP-IN-IF and STOP RUN ends the run
      *>             unit before AFTER-IF-MUST-NOT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB397LEGAL85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1.
       01 K PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "M1" GO TO SEQ-A.
       SEQ-A.
           IF N = 1 GO TO SEQ-B END-IF DISPLAY "A-MUST-NOT".
       SEQ-B.
           IF N = 2
               DISPLAY "B-NOT"
           ELSE
               DISPLAY "B-ELSE"
               GO TO SEQ-C
           END-IF
           DISPLAY "B-MUST-NOT".
       SEQ-C.
           MOVE 0 TO K
           GO TO SEQ-D SEQ-E DEPENDING ON K
           DISPLAY "C-FALL"
           MOVE 2 TO K
           GO TO SEQ-D SEQ-E DEPENDING ON K
           DISPLAY "C-MUST-NOT".
       SEQ-D.
           DISPLAY "D-MUST-NOT".
       SEQ-E.
           DISPLAY "E".
           PERFORM EX-PARA.
           DISPLAY "AFTER-EXIT".
           STOP "OPERATOR-NOTICE" DISPLAY "AFTER-STOP-LITERAL".
           IF N = 2 STOP RUN ELSE DISPLAY "ELSE-BRANCH" END-IF.
           IF N = 1 DISPLAY "STOP-IN-IF" STOP RUN END-IF
           DISPLAY "AFTER-IF-MUST-NOT".
       EX-PARA.
           EXIT.
