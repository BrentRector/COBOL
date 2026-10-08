       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1927AL.
      *> kb/Work PB1927 - ISO 13.18.57.4 GR7 d) 4. + GR6 c) 2.: CH-Y has no OR PAGE phrase and CH-M has one,
      *> so GR7 d) 4. names no OR PAGE heading at YR's level or higher for CF-Y, whose upper limit is FIRST
      *> DETAIL (2) and CF-Y LINE 2 is legal (13.18.35.3 SR6 c)). CF-Y at the level YR causes the page advance:
      *> the OR PAGE heading CH-M is BELOW it, so it is not reprinted, and CF-Y stands on line 2 alone. A build
      *> that reprinted CH-M overprinted it with CF-Y on one line.
      *>   cite.py: OK  13.18.57.4 6) c) 2.   cite.py: OK  13.18.57.4 7) d)   (General rules)
      *> LAYOUT (all of these): PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2; LAST DETAIL and
      *> FOOTING default to the page limit 8. The page heading PG stands on line 1. The read-back numbers
      *> each physical line of a page; a form feed prints "(page)" and restarts the numbering.
      *> DERIVATION. Page 1: PG 1, "Y=1" 2, "M=1" 3, details 1-4 at 4-7. MOVE 2 TO YR, GENERATE: CF-M "TM1"
      *>  at 8; CF-Y (absolute 2, column 6) is above the current line, so it takes the page advance. Page 2:
      *>  PG 1, then CF-Y "TY1" on line 2 (column 6) with nothing reprinted; CH-Y "Y=2" 3, CH-M "M=1" 4, detail
      *>  "D 5" 5, CF-M "TM1" 6 at TERMINATE; CF-Y "TY2" needs line 2 again: page 3, PG 1, "TY2" 2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1927AL.TXT".
           SELECT CHK ASSIGN TO "PB1927AL.TXT".
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
       RD  R CONTROLS ARE YR MO
           PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2.
       01  PH-1 TYPE PAGE HEADING.
           02  LINE 1.
               03  COLUMN 1 PIC XX VALUE "PG".
       01  CH-Y TYPE CONTROL HEADING FOR YR LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "Y=".
           03  COLUMN 3 PIC 9 SOURCE YR.
       01  CH-M TYPE CONTROL HEADING FOR MO OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "M=".
           03  COLUMN 3 PIC 9 SOURCE MO.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
           03  COLUMN 3 PIC 9 SOURCE WS-N.
       01  CF-M TYPE CF FOR MO LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "TM".
           03  COLUMN 3 PIC 9 SOURCE MO.
       01  CF-Y TYPE CF FOR YR LINE 2.
           03  COLUMN 6 PIC XX VALUE "TY".
           03  COLUMN 8 PIC 9 SOURCE YR.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           PERFORM 4 TIMES
               ADD 1 TO WS-N
               GENERATE D1
           END-PERFORM.
           MOVE 2 TO YR.
           ADD 1 TO WS-N.
           GENERATE D1.
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
