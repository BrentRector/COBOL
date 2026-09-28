       >>TURN EC-REPORT-VARYING CHECKING ON
      *> kb/Work PB1305 - a report VARYING clause whose FROM / BY is an
      *> arithmetic expression. The engine held each counter in a C#
      *> `long` local, so any expression with an operator (which the
      *> numeric renderer widens to Int128) or a nonzero-scale operand
      *> failed the generated-C# build (CS0266) - a compiler crash on
      *> conforming source - and a noninteger value never raised
      *> EC-REPORT-VARYING.
      *>
      *> "VARYING { data-name-1 [ FROM arithmetic-expression-1 ]
      *> [ BY arithmetic-expression-2 ] } ..."   (§13.18.64.2)
      *> "Each entry containing a VARYING clause establishes an
      *> independent temporary integer data item that shall be large
      *> enough to contain the maximum expected value."
      *>   cite.py: OK  §13.18.64.4 1)  (General rules)
      *> "For the first occurrence, the value of arithmetic-expression-1
      *> is moved to data-name-1."
      *>   cite.py: OK  §13.18.64.4 3)  (General rules)
      *> "For the second and subsequent occurrences, the value of
      *> arithmetic-expression-2 is added to data-name-1."
      *>   cite.py: OK  §13.18.64.4 3)  (General rules)
      *> "If the evaluation of arithmetic-expression-1 or
      *> arithmetic-expression-2 produces a noninteger value and the
      *> VARYING clause was specified in a report description entry,
      *> the EC-REPORT-VARYING exception condition is set to exist"
      *>   cite.py: OK  §13.18.64.4 5)  (General rules)
      *>
      *> DERIVATION.
      *> DET-A  FROM 2 * 3 - 5 BY 4 - 2: 1, then 1 + 2 = 3, then 5 ->
      *>        "1 3 5".
      *> DET-B  FROM 1 BY WS-F (PIC 9V9 VALUE 2.0 - scale 1, an integer
      *>        value): 1 3 5 -> "1 3 5".
      *> DET-C  FROM WS-D (COMP-2 VALUE 4, a float that is an integer)
      *>        BY 7 - 2 * 3: 4 5 6 -> "4 5 6".
      *> DET-D  FROM 7 / 2 = 3.5, a noninteger: EC-REPORT-VARYING is
      *>        raised (checking is ON), the declarative displays its
      *>        name, and RESUME AT NEXT STATEMENT continues after the
      *>        GENERATE, which was unsuccessful. Its print line is
      *>        undefined (GR5), so only lines 1-3 of the report file
      *>        (DET-A, DET-B, DET-C) are read back and displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1305V.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1305v.txt".
           SELECT CHK ASSIGN TO "pb1305v.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-V.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-F    PIC 9V9 VALUE 2.0.
       01  WS-D    USAGE COMP-2 VALUE 4.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-V.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING K FROM 2 * 3 - 5
                   BY 4 - 2 SOURCE K.
       01  DET-B TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING J FROM 1 BY WS-F
                   SOURCE J.
       01  DET-C TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING L FROM WS-D
                   BY 7 - 2 * 3 SOURCE L.
       01  DET-D TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING M FROM 7 / 2
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
           GENERATE DET-C.
           GENERATE DET-D.
           DISPLAY "AFTER DET-D".
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
           IF WS-LN < 4
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:5) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
