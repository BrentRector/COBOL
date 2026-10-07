      *> kb/Work PB1270 (residue) - the LEGAL side of ISO 13.18.39.4 GR1/GR2: RELATIVE first lines on the boundary of the region
      *> their TYPE allows. conformance:negative/pb1270-*-relative-* are the refused side.
      *> PAGE LIMIT 12, HEADING 2, FIRST DETAIL 5, LAST DETAIL 9, FOOTING 9 (ascending; 13.18.39.3 SR6).
      *> 13.18.35.4 GR5 b) fixes where a relative first line prints: a report heading, and a page heading with no report heading
      *> before it, on HEADING + integer-2 - 1; the first body group on a page on FIRST DETAIL; a page footing on FOOTING +
      *> integer-2; a report footing after a page footing on LINE-COUNTER + integer-2 (a page heading after a report heading the
      *> same). GR2 c): no line higher than HEADING (2): RH PLUS 1 prints ON it. GR2 d): the report heading and page heading
      *> terminate before FIRST DETAIL (5): PH PLUS 1 after RH prints on 3. GR2 g): a page footing begins after FOOTING (9):
      *> PF PLUS 1 prints on 10, its second line PLUS 1 on 11. GR2 a): nothing below the page limit (12): RF PLUS 1 after the
      *> page footing prints ON 12. D1 prints on FIRST DETAIL 5 (GR5 b 3.), its second line PLUS 4 on 9 = LAST DETAIL.
      *> Nothing here is newer than COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1270R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1270R.TXT".
           SELECT CHK ASSIGN TO "PB1270R.TXT".
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
       RD  R1 PAGE LIMIT IS 12 LINES HEADING 2 FIRST DETAIL 5
           LAST DETAIL 9 FOOTING 9.
       01  RH1 TYPE RH LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH1 TYPE PH LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "PH".
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(2) VALUE "D1".
           03  LINE PLUS 4.
               05  COLUMN 1 PIC X(2) VALUE "D9".
       01  PF1 TYPE PF.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(2) VALUE "PF".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(2) VALUE "P2".
       01  RF1 TYPE RF LINE PLUS 1.
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
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = SYM-X0D
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
