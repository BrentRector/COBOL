      *> kb/Work PB1270 + PB1222 - the LEGAL side of ISO 13.18.39.3 SR5/SR6 and 13.18.35.3 SR6 c): every group on the boundary of
      *> the region its TYPE allows. conformance:negative/pb1270-* are the refused side.
      *> 13.18.39.3 SR6: the integers "shall be in ascending order, with equality allowed."   cite.py: OK  13.18.39.3 6)
      *> 13.18.35.3 SR6 c): no absolute line "above the upper limit or below the lower limit allowed for the report group."
      *>   cite.py: OK  13.18.35.3 6) c)  (Syntax rules)
      *> The limits are 13.18.57.4 GR7 and GR8. cite.py: OK  13.18.57.4 7)  and  13.18.57.4 8) c)  (General rules)
      *> Nothing here is newer than COBOL-85.
      *> DERIVATION. PAGE LIMIT 12, HEADING 2, FIRST DETAIL 4, LAST DETAIL 9, FOOTING 9: ascending, LAST DETAIL = FOOTING (equality
      *> allowed), both below the page limit. The report heading's upper limit is HEADING, 2 (GR7 a): RH LINE 2 is ON it. The page
      *> heading's lower limit is FIRST DETAIL - 1 = 3 (GR8 c) and, with a report heading on the same page, its upper limit is the
      *> line after the report heading's last line, also 3 (GR7 b): PH LINE 3 is ON both. The detail's limits are FIRST DETAIL 4 and
      *> LAST DETAIL 9 (GR7 c, GR8 e): LINE 4 and LINE 9 are ON them, in increasing order. The page footing's upper limit is
      *> FOOTING + 1 = 10 (GR7 e): PF LINE 10 is ON it. The report footing follows the page footing on LINE 11 (GR7 g).
      *> GENERATE D1 prints RH (the first GENERATE), then PH, then D1 on the same page; TERMINATE prints PF and RF. Blank lines are
      *> not displayed, so: RH on 2, PH on 3, D1 on 4 and 9, PF on 10, RF on 11.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1270P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1270P.TXT".
           SELECT CHK ASSIGN TO "PB1270P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 4
           LAST DETAIL 9 FOOTING 9.
       01  RH1 TYPE RH LINE 2.
           03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH1 TYPE PH LINE 3.
           03  COLUMN 1 PIC X(2) VALUE "PH".
       01  D1 TYPE DE.
           03  LINE 4.
               05  COLUMN 1 PIC X(2) VALUE "D1".
           03  LINE 9.
               05  COLUMN 1 PIC X(2) VALUE "D9".
       01  PF1 TYPE PF LINE 10.
           03  COLUMN 1 PIC X(2) VALUE "PF".
       01  RF1 TYPE RF LINE 11.
           03  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
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
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
