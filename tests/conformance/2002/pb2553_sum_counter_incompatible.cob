       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
      *> kb/Work PB2553 - A REFERENCE TO A SUM COUNTER HOLDING NON-NUMERIC
      *> CHARACTERS IS INCOMPATIBLE DATA.
      *>
      *> 8.4.3.3.4 GR5 puts the character MOVEd into CF-T (2:1) in the
      *> counter (see 85/pb2553_sum_counter_character_cell). 14.6.13.2
      *> rule 2: "When the content of a numeric sending item ... is
      *> referenced during the execution of a statement and the content of
      *> that sending operand would evaluate to false in a numeric class
      *> condition, the result of the reference is undefined and an
      *> EC-DATA-INCOMPATIBLE exception condition is set to exist, except
      *> ... a sending item is referenced in a class condition". So the
      *> class condition is exempt (L1), the ADD that reads the counter
      *> raises (L2), and so does the GENERATE whose 13.18.54.4 GR3 addition
      *> into the counter is "consistent with the general rules of the ADD
      *> statement" (L3). After a numeric MOVE the content is valid and its
      *> reference raises nothing (L4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2553B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB2553B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X     PIC 9999 VALUE 1234.
       01  WS-D     PIC 9999 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  CF-T COLUMN 1  PIC 9999 SUM WS-X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE "X" TO CF-T (2:1).
           DISPLAY "L1 class condition (exempt)".
           IF CF-T IS NUMERIC
               DISPLAY "   numeric"
           ELSE
               DISPLAY "   not numeric"
           END-IF.
           DISPLAY "L2 ADD".
           ADD 1 TO CF-T.
           DISPLAY "L3 GENERATE".
           MOVE "X" TO CF-T (2:1).
           GENERATE DET.
           DISPLAY "L4 MOVE".
           MOVE 5 TO CF-T.
           MOVE CF-T TO WS-D.
           DISPLAY "   D=[" WS-D "]".
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
