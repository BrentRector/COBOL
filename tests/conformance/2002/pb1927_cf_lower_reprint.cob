       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1927LW.
      *> kb/Work PB1927 - ISO 13.18.57.4 GR6 c) 2. with GR7 d) 4.: a control footing at a LOWER control
      *> level than an OR PAGE control heading causes the page advance. GR6 c) reprints the OR PAGE heading
      *> "provided that the page advance did not take place just before the printing of a control footing at a
      *> lower control level"; GR7 d) 4. gives that footing the upper limit "the line following the last line of
      *> the lowest-level control heading with an OR PAGE phrase at the same level as the control footing, or
      *> higher". The two read together (the proviso's "lower" is the HEADING's level: a heading BELOW the
      *> footing is not reprinted, one at the footing's level or above it is) put the headings of the groups
      *> that are still open above the footing, docs/CONFORMANCE.md A.4.11.
      *>   cite.py: OK  13.18.57.4 6) c) 2.   cite.py: OK  13.18.57.4 7) d)   (General rules)
      *> LAYOUT (all of these): PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2; LAST DETAIL and
      *> FOOTING default to the page limit 8. The page heading PG stands on line 1. The read-back numbers
      *> each physical line of a page; a form feed prints "(page)" and restarts the numbering.
      *> DERIVATION. CONTROLS ARE YR MO; CH-Y and CH-M are both OR PAGE, CF-M is at the lower level MO.
      *>  Page 1: PG 1, CH-Y "Y=1" 2 (the first body group, FIRST DETAIL), CH-M "M=1" 3, details 1-5 at 4-8.
      *>  MOVE 2 TO MO, GENERATE: the break is at MO. CF-M (printed with the prior MO, 1) needs line 9, past
      *>  FOOTING 8, so CF-M causes the page advance. Page 2: PG 1; the OR PAGE headings at CF-M's level (MO)
      *>  and above (YR), major to minor: "Y=1" 2, "M=1" 3; then CF-M "TM1" 4 (the d) 4. upper limit);
      *>  then the break's CH-M "M=2" 5 and detail 6 "D 6" at 6. TERMINATE: CF-M "TM2" 7, CF-Y "TY1" 8.
      *> A build that skipped the headings ABOVE the footing printed PG, M=1, TM1, M=2: TM1 stood on line 3,
      *> above its upper limit 4, and Y=1 was missing.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1927LW.TXT".
           SELECT CHK ASSIGN TO "PB1927LW.TXT".
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
       01  CF-M TYPE CF FOR MO LINE PLUS 1.
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
