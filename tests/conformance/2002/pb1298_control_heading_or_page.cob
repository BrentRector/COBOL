       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1298OP.
      *> kb/Work PB1298 + PB1248 - ISO 13.18.57.2 Format 2, rendered:
      *>   {CONTROL HEADING | CH} [ [ON | FOR] {data-name-1 | FINAL} [OR PAGE] ]
      *>   {CONTROL FOOTING | CF} [ [ON | FOR] {data-name-2 | FINAL} ]
      *> ON and FOR are optional words; OR PAGE belongs to the heading.
      *>   cite.py: OK  13.18.57.2  (General formats)
      *> 13.18.57.4 GR6 c): "The OR PAGE phrase causes the associated control
      *> heading to be printed in addition after each page advance, following any
      *> page heading, provided that the page advance did not take place just
      *> before the printing of a control footing at a lower control level."
      *>   cite.py: OK  13.18.57.4 6) c)  (General rules)
      *> 13.18.57.4 GR7 d) 1.: a control heading at the highest OR PAGE level has
      *> the FIRST DETAIL integer as its upper limit, so the reprinted heading is
      *> the first body group on the new page; GR7 d) 3.: the detail follows the
      *> last line of the lowest-level OR PAGE heading.
      *>   cite.py: OK  13.18.57.4 7) d)  (General rules)
      *> Before the phrase had a grammar surface `TYPE CH ON CTL OR PAGE` was a
      *> parse error and nothing reprinted a heading.
      *> DERIVATION: PAGE LIMIT IS 8 LINES FIRST DETAIL 2 (HEADING 1; LAST DETAIL
      *> and FOOTING default to the limit 8). CONTROL IS CTL.
      *>  Page 1: PH at line 1 "PG". First GENERATE (CTL "A"): CH A at FIRST
      *>   DETAIL, line 2 "H=A"; detail 1 at line 3; details 2-6 at lines 4-8.
      *>   LINE-COUNTER is 8.
      *>  7th GENERATE: the detail's trial sum is 8 + 1 = 9 > LAST DETAIL 8, so
      *>   the page fit fails: page advance, PH at line 1, then (GR6 c) the OR
      *>   PAGE heading "H=A" - the first body group on the page, line 2 - then
      *>   detail 7 at line 3 (LINE-COUNTER 3).
      *>  CTL becomes "B", 8th GENERATE: control break. The control footing
      *>   (CF FOR CTL) prints with the PRIOR value, line 4 "F=A"; then the
      *>   control heading for "B", line 5 "H=B"; then detail 8 at line 6. No
      *>   page advance, so no reprint.
      *>  TERMINATE: CF for "B" at line 7 "F=B".
      *> The read-back numbers each physical line of a page; a form feed prints
      *> "(page)" and restarts the numbering. WITHOUT the OR PAGE phrase page 2
      *> would hold PG at line 1 and detail 7 at line 2, with no "H=A".
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1298OP.TXT".
           SELECT CHK ASSIGN TO "PB1298OP.TXT".
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
       01  CTL     PIC X     VALUE "A".
       01  WS-N    PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R CONTROL IS CTL PAGE LIMIT IS 8 LINES FIRST DETAIL 2.
       01  PH-1 TYPE PAGE HEADING.
           02  LINE 1.
               03  COLUMN 1 PIC XX VALUE "PG".
       01  CH-1 TYPE CONTROL HEADING ON CTL OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "H=".
           03  COLUMN 3 PIC X SOURCE CTL.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
           03  COLUMN 3 PIC 9 SOURCE WS-N.
       01  CF-1 TYPE CF FOR CTL LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "F=".
           03  COLUMN 3 PIC X SOURCE CTL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           PERFORM 7 TIMES
               ADD 1 TO WS-N
               GENERATE D1
           END-PERFORM.
           MOVE "B" TO CTL.
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
