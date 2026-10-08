       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1927FN.
      *> kb/Work PB1927 - ISO 13.18.57.4 GR6 c) 2. with GR7 d) 4., FINAL: "FINAL, if specified, is associated
      *> with the highest level in the hierarchy" (13.18.16.4 GR2), so CF FINAL is the highest-level footing and
      *> only a heading at the FINAL level is at its level or above. TERMINATE: CF-Y fits on line 8 and CF FINAL
      *> causes the page advance; page 2 holds PG and the FINAL heading, and no "Y=" heading.
      *>   cite.py: OK  13.18.57.4 6) c) 2.   cite.py: OK  13.18.57.4 7) d)   (General rules)
      *> LAYOUT (all of these): PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2; LAST DETAIL and
      *> FOOTING default to the page limit 8. The page heading PG stands on line 1. The read-back numbers
      *> each physical line of a page; a form feed prints "(page)" and restarts the numbering.
      *> DERIVATION. CONTROLS ARE FINAL YR; CH-F and CH-Y are both OR PAGE.
      *>  Page 1: PG 1, "F" 2, "Y=1" 3, details 1-4 at 4-7, TERMINATE: CF-Y "TY1" at 8. CF FINAL "TF" needs
      *>  line 9: page advance caused by a footing at the FINAL level. Page 2: PG 1, "F" 2, "TF" 3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1927FN.TXT".
           SELECT CHK ASSIGN TO "PB1927FN.TXT".
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
       01  WS-LINE PIC X(20) VALUE SPACES.
       01  YR      PIC 9     VALUE 1.
       01  MO      PIC 9     VALUE 1.
       01  WS-N    PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R CONTROLS ARE FINAL YR
           PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2.
       01  PH-1 TYPE PAGE HEADING.
           02  LINE 1.
               03  COLUMN 1 PIC XX VALUE "PG".
       01  CH-F TYPE CONTROL HEADING FINAL OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "F".
       01  CH-Y TYPE CONTROL HEADING FOR YR OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "Y=".
           03  COLUMN 3 PIC 9 SOURCE YR.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
           03  COLUMN 3 PIC 9 SOURCE WS-N.
       01  CF-Y TYPE CF FOR YR LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "TY".
           03  COLUMN 3 PIC 9 SOURCE YR.
       01  CF-F TYPE CF FINAL LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "TF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           PERFORM 4 TIMES
               ADD 1 TO WS-N
               GENERATE D1
           END-PERFORM.
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
                   DISPLAY "(page)"
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:10) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
