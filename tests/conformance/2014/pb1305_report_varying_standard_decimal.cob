       >>TURN EC-REPORT-VARYING CHECKING ON
      *> kb/Work PB1305 - the standard-decimal lane of a report VARYING
      *> FROM / BY expression. Under ARITHMETIC IS STANDARD-DECIMAL an
      *> arithmetic expression is a standard-decimal intermediate
      *> (§11.9.5.2 GR3), and the VARYING clause's noninteger test
      *> (§13.18.64.4 GR5) applies to it as to any other value.
      *> "If the STANDARD-DECIMAL phrase is specified, the techniques
      *> used in handling arithmetic expressions, arithmetic statements,
      *> the SUM clause, and integer and numeric functions shall be as
      *> described for standard-decimal arithmetic"
      *>   cite.py: OK  §11.9.5.2 3)  (General rules)
      *> "Each entry containing a VARYING clause establishes an
      *> independent temporary integer data item that shall be large
      *> enough to contain the maximum expected value."
      *>   cite.py: OK  §13.18.64.4 1)  (General rules)
      *> "If the evaluation of arithmetic-expression-1 or
      *> arithmetic-expression-2 produces a noninteger value and the
      *> VARYING clause was specified in a report description entry,
      *> the EC-REPORT-VARYING exception condition is set to exist"
      *>   cite.py: OK  §13.18.64.4 5)  (General rules)
      *>
      *> DERIVATION.
      *> DET-A  FROM 12 / 4 = 3 BY 6 / 3 = 2 (integers): 3 5 7 ->
      *>        "3 5 7".
      *> DET-B  BY 7 / 2 = 3.5, a noninteger: EC-REPORT-VARYING is
      *>        raised, the declarative displays its name, and RESUME AT
      *>        NEXT STATEMENT continues after the unsuccessful GENERATE.
      *>        Its print line is undefined, so only line 1 is shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1305S.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1305s.txt".
           SELECT CHK ASSIGN TO "pb1305s.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-V.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-V.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING K FROM 12 / 4
                   BY 6 / 3 SOURCE K.
       01  DET-B TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING M FROM 1 BY 7 / 2
                   SOURCE M.
       PROCEDURE DIVISION.
       DECLARATIVES.
       VX SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-VARYING.
       VX-P.
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT PRT.
           INITIATE R-V.
           GENERATE DET-A.
           GENERATE DET-B.
           DISPLAY "AFTER DET-B".
           TERMINATE R-V.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LN = 1
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:5) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
