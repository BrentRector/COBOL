      *> kb/Work PB1299 - ISO 13.18.57.3 SR15 is met by ANY body group, and SR16 ('If no GENERATE data-name statements are specified in the
      *> procedure division, the report description need not contain a DETAIL') leaves a report of one CONTROL FOOTING legal: a summary
      *> report, driven by GENERATE report-name.   cite.py: OK  13.18.57.3 15), 16)   Nothing here is newer than COBOL-85.
      *> DERIVATION. GENERATE R1 on a report with no detail processes the control footings; TERMINATE prints the FINAL control footing
      *> once on its absolute line 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1299S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1299S.TXT".
           SELECT CHK ASSIGN TO "PB1299S.TXT".
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
       RD  R1 CONTROLS ARE FINAL PAGE LIMIT IS 10 LINES
           HEADING 1 FIRST DETAIL 2.
       01  CFF TYPE CF FINAL LINE 3.
           03  COLUMN 1 PIC X(2) VALUE "FF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE R1.
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
