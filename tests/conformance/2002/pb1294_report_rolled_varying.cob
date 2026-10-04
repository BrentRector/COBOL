      *> kb/Work PB1294 - ISO 13.18.54.4 GR6 with 13.18.64.4 GR3: a rolled total of an entry whose SOURCE operand is a
      *> VARYING counter. The value added is "that of the operand of the SOURCE ... clause" (GR6), and the operand is
      *> the counter, whose value is its occurrence's.
      *>   cite.py: OK  13.18.54.4 6)  (General rules)
      *> 13.18.64.4 GR3: "For the first occurrence, the value of arithmetic-expression-1 is moved to data-name-1 ... For
      *>   the second and subsequent occurrences, the value of arithmetic-expression-2 is added to data-name-1."
      *>   cite.py: OK  13.18.64.4 3)  (General rules)
      *> GR7 b): data-name-1 in the SAME report group description is added "during the processing of the current report
      *>   group before any of the report group's lines are printed".   cite.py: OK  13.18.54.4 7) b)  (General rules)
      *> SR4 b): the addend SEQ is a repeating item with one more level of repetition than the subject TOT.
      *>   cite.py: OK  13.18.54.3 4) b)  (Syntax rules)
      *>
      *> DERIVATION. SEQ is PIC 99, OCCURS 3 TIMES STEP 3, VARYING I FROM 4 BY 2, SOURCE I: its occurrences hold 4, 6, 8
      *> (FROM, then BY added each time), printed at columns 1, 4, 7. TOT, PIC 999 at column 12, SUMs SEQ in the same
      *> group, so each occurrence is added (GR8 b) when DET is processed: 4 + 6 + 8 = 18. The only line:
      *> `04 06 08   018`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1294VRY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1294VRY.TXT".
           SELECT CHK ASSIGN TO "PB1294VRY.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R.
       01  DET TYPE DE LINE PLUS 1.
           05  SEQ COLUMN 1 PIC 99 OCCURS 3 TIMES STEP 3
               VARYING I FROM 4 BY 2 SOURCE I.
           05  TOT COLUMN 12 PIC 999 SUM SEQ.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DET.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:16) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
