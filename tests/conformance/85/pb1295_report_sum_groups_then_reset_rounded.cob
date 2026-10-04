      *> kb/Work PB1295 - the LEGAL side of ISO 13.18.54.2: the SUM clause is the repeated SUM group followed by ONE RESET phrase and ONE
      *> rounded-phrase. conformance:negative/pb1295-sum-*-between-groups are the refused side. cite.py: OK  13.18.54.2
      *> 13.18.54.3 SR1: "The whole clause is referred to as a SUM clause even though the SUM keyword may appear more than once."
      *> Nothing here is newer than COBOL-85.
      *> DERIVATION. The two SUM groups are two addends of one counter (13.18.54.4 GR1, GR3): WS-A 2 + WS-B 3 = 5 is added at each GENERATE, and
      *> RESET ON FINAL resets only at the end of the report, so the running total printed after the first GENERATE is 05 and after the second
      *> 10. The two details print on the first two lines of the page (FIRST DETAIL defaults to 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1295P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1295P.TXT".
           SELECT CHK ASSIGN TO "PB1295P.TXT".
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
       01  WS-A PIC 9 VALUE 2.
       01  WS-B PIC 9 VALUE 3.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL PAGE LIMIT IS 10 LINES.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 99 SUM WS-A SUM WS-B RESET ON FINAL ROUNDED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
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
