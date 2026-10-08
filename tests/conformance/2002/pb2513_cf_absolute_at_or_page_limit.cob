       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2513AT.
      *> kb/Work PB2513 - ISO 13.18.57.4 GR7 d) 2.: the upper limit of a control heading at a LOWER level than
      *> the highest OR PAGE control heading is "the line following the last line of the next higher-level control
      *> heading"; GR7 d) 4.: a control footing's is "the line following the last line of the lowest-level control
      *> heading with an OR PAGE phrase at the same level as the control footing, or higher". CH-Y (line 2) then
      *> CH-M (line 3) stand at the top of every page, so CF-M's upper limit is 4 and CF-M LINE 4 is the EDGE of
      *> 13.18.35.3 SR6 c), "no line appears above the upper limit". The same program with LINE 3 is the
      *> negative pb2513-cf-absolute-above-or-page-limit: a build that placed every CH-M first line at FIRST
      *> DETAIL computed 3 for the limit and accepted LINE 3 (CF-M then overprinted the reprinted CH-M).
      *>   cite.py: OK  13.18.57.4 7) d)   cite.py: OK  13.18.35.3 6) c)
      *> LAYOUT: PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2; LAST DETAIL and FOOTING default to the
      *> page limit 8. The page heading PG stands on line 1. The read-back numbers each physical line of a
      *> page; a form feed prints "(page)" and restarts the numbering.
      *> DERIVATION. Page 1: PG 1, "Y=1" 2, "M=1" 3, details 1-5 at 4-8. MOVE 2 TO MO, GENERATE: CF-M is
      *>  absolute line 4, above the current line 8: page advance (13.18.35.4 GR4). Page 2: PG 1, the OR PAGE
      *>  headings at CF-M's level and above, "Y=1" 2, "M=1" 3, then CF-M "TM1" on line 4, the break's CH-M
      *>  "M=2" 5 and detail "D 6" 6. TERMINATE: CF-M "TM2" is line 4 again, above line 6: page 3, PG 1, "Y=1" 2,
      *>  "M=2" 3 (the control values now current), "TM2" 4, then CF-Y "TY1" 5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB2513AT.TXT".
           SELECT CHK ASSIGN TO "PB2513AT.TXT".
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
       01  CH-Y TYPE CONTROL HEADING FOR YR OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "Y=".
           03  COLUMN 3 PIC 9 SOURCE YR.
       01  CH-M TYPE CONTROL HEADING FOR MO OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "M=".
           03  COLUMN 3 PIC 9 SOURCE MO.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
           03  COLUMN 3 PIC 9 SOURCE WS-N.
       01  CF-M TYPE CF FOR MO LINE 4.
           03  COLUMN 1 PIC XX VALUE "TM".
           03  COLUMN 3 PIC 9 SOURCE MO.
       01  CF-Y TYPE CF FOR YR LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "TY".
           03  COLUMN 3 PIC 9 SOURCE YR.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           PERFORM 5 TIMES
               ADD 1 TO WS-N
               GENERATE D1
           END-PERFORM.
           MOVE 2 TO MO.
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
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
