      *> kb/Work PB1270 - ISO 13.18.57.4 GR7 d) 3. with 13.18.35.3 SR6 c): with an OR PAGE control heading in the report, a detail's
      *> upper limit is "the line following the last line of the lowest-level control heading that has an OR PAGE phrase."
      *> cite.py: OK  13.18.57.4 7)  (General rules)   and   13.18.35.3 6) c)  (Syntax rules)
      *> OR PAGE is a COBOL 2002 phrase of TYPE CONTROL HEADING (13.18.57.2), so this is the introducing-edition witness;
      *> conformance:negative/pb1270-detail-inside-or-page-heading is the refused side.
      *> DERIVATION. PAGE LIMIT IS 10 LINES FIRST DETAIL 3 (HEADING 1; LAST DETAIL and FOOTING default to 10, GR3). The control
      *> heading CH1 has two relative lines. The first GENERATE is the first body group of page 1, so the control heading prints
      *> first (13.18.57.4 GR6 c 1.) with its first line on FIRST DETAIL, 3 (13.18.35.4 GR5 b 3.), the second on 4. The detail's
      *> upper limit is the line after that, 5, so DE LINE 5 is ON it, and prints there: page fit succeeds (5 is after the
      *> line-counter 4, GR4 b). No control footing, page heading or footing is defined. Blank lines are not displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1270O.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1270O.TXT".
           SELECT CHK ASSIGN TO "PB1270O.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WN PIC 9 VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 CONTROL IS WN PAGE LIMIT IS 10 LINES FIRST DETAIL 3.
       01  CH1 TYPE CH ON WN OR PAGE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(2) VALUE "C1".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(2) VALUE "C2".
       01  D1 TYPE DE LINE 5.
           03  COLUMN 1 PIC X VALUE "D".
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
