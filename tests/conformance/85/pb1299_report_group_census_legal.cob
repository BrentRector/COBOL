      *> kb/Work PB1299 - the LEGAL side of ISO 13.18.57.3 SR13, SR14 and SR15: each of REPORT HEADING, PAGE HEADING, PAGE FOOTING
      *> and REPORT FOOTING written ONCE, one CONTROL HEADING and one CONTROL FOOTING for each of two controls (FINAL and WS-K),
      *> and the body groups (DETAIL, CONTROL HEADING, CONTROL FOOTING) of SR15.   cite.py: OK  13.18.57.3 13), 14), 15)
      *> conformance:negative/pb1299-* are the refused side. Nothing here is newer than COBOL-85.
      *> DERIVATION. Every group's first line is absolute, so the printed line numbers are the LINE integers: RH 1, PH 2, the two
      *> control headings 4 and 5 (the first GENERATE starts every control, 13.18.57.4 GR6), the detail 6, then at TERMINATE the
      *> control footings minor-first, WS-K 7 and FINAL 8 (13.18.57.4 GR6), the page footing 19 and the report footing 20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1299P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1299P.TXT".
           SELECT CHK ASSIGN TO "PB1299P.TXT".
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
       01  WS-K PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL WS-K PAGE LIMIT IS 20 LINES
           HEADING 1 FIRST DETAIL 3 FOOTING 18.
       01  RH1 TYPE RH LINE 1.
           03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH1 TYPE PH LINE 2.
           03  COLUMN 1 PIC X(2) VALUE "PH".
       01  CHF TYPE CH FINAL LINE 4.
           03  COLUMN 1 PIC X(2) VALUE "HF".
       01  CHK TYPE CH WS-K LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "HK".
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "D1".
       01  CFK TYPE CF WS-K LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "FK".
       01  CFF TYPE CF FINAL LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "FF".
       01  PF1 TYPE PF LINE 19.
           03  COLUMN 1 PIC X(2) VALUE "PF".
       01  RF1 TYPE RF LINE 20.
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
