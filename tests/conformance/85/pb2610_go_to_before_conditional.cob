      *> kb/Work PB2610 - a Format 1 GO TO (and a STOP RUN) written directly before a CONDITIONAL statement is the
      *> last statement of its consecutive sequence of imperative statements, so it is legal. The rules carry no
      *> edition qualifier and every shape below is in X3.23-1985, so this is the single positive golden at the
      *> earliest edition; StatementSequencePlacementTests runs the same shapes at all four editions, and the
      *> imperative-follower case stays refused (negative pb397-go-to-not-last).
      *>
      *> 14.9.17.3 SR2: "If a GO TO statement represented by format 1 appears in a consecutive sequence of
      *>   imperative statements within a sentence, it shall appear as the last statement in that sequence."
      *> 14.5.1: "Any statement with a conditional phrase that is not terminated by its explicit scope terminator
      *>   is a conditional statement." So each statement after a GO TO below (an IF with no END-IF, an EVALUATE
      *>   with no END-EVALUATE, an ADD with ON SIZE ERROR and no END-ADD, a SEARCH with AT END and WHEN and no
      *>   END-SEARCH, a STRING with ON OVERFLOW and no END-STRING, a sequential READ with AT END and a relative
      *>   READ with INVALID KEY, neither with END-READ) is conditional and ends the imperative sequence; the GO TO
      *>   is its last statement. P-PHRASE puts the same shape inside an IF's THEN phrase, whose statement-1 may
      *>   end in a conditional statement (14.9.19.3 SR1); the END-IF ends both the ADD and the IF (14.5.3.2).
      *> 14.9.42.3 SR1: "The STOP statement shall be specified only as the last statement in any discreet block of
      *>   code." Read as the same consecutive sequence of imperative statements (docs/CONFORMANCE.md D-SEQ), so
      *>   STOP RUN before the conditional IF in P-STOP is the last statement of its block.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN: each paragraph prints its name and its GO TO
      *> transfers control before the conditional statement after it is reached (14.9.17.4 GR1), so no
      *> "MUST-NOT" line prints and neither file is touched; P-STOP prints STOP and STOP RUN ends the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2610GOCOND85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQF ASSIGN TO "PB2610SF.DAT".
           SELECT RELF ASSIGN TO "PB2610RF.DAT"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD SEQF.
       01 SF-REC PIC X(4).
       FD RELF.
       01 RF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1.
       01 RK PIC 9(4) VALUE 1.
       01 S PIC X(4).
       01 TB.
          05 T-E PIC X OCCURS 3 INDEXED BY TX.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "M1"
           GO TO P-EVAL
           IF N = 1 DISPLAY "IF-MUST-NOT".
       P-EVAL.
           DISPLAY "EVAL"
           GO TO P-SIZE
           EVALUATE N WHEN 1 DISPLAY "EVAL-MUST-NOT".
       P-SIZE.
           DISPLAY "SIZE"
           GO TO P-SEARCH
           ADD 1 TO N ON SIZE ERROR DISPLAY "SIZE-MUST-NOT".
       P-SEARCH.
           DISPLAY "SEARCH"
           SET TX TO 1
           GO TO P-STRING
           SEARCH T-E AT END DISPLAY "SEARCH-MUST-NOT"
               WHEN T-E (TX) = "B" DISPLAY "WHEN-MUST-NOT".
       P-STRING.
           DISPLAY "STRING"
           GO TO P-READ
           STRING "AB" DELIMITED BY SIZE INTO S
               ON OVERFLOW DISPLAY "STRING-MUST-NOT".
       P-READ.
           DISPLAY "READ"
           GO TO P-RELREAD
           READ SEQF AT END DISPLAY "READ-MUST-NOT".
       P-RELREAD.
           DISPLAY "RELREAD"
           GO TO P-PHRASE
           READ RELF INVALID KEY DISPLAY "RELREAD-MUST-NOT".
       P-PHRASE.
           IF N = 1
               DISPLAY "PHRASE"
               GO TO P-STOP
               ADD 1 TO N ON SIZE ERROR DISPLAY "PHRASE-MUST-NOT"
           END-IF.
       P-STOP.
           DISPLAY "STOP"
           STOP RUN
           IF N = 1 DISPLAY "STOP-MUST-NOT".
