      *> PB1685 - ISO 13.18.54.4 GR1: a SUM counter takes its digits, integral and
      *>   fractional, from the entry PICTURE; a numeric-edited PIC 99.99 keeps
      *>   two decimals. 2.75 added twice must print 05.50 (was 04.00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1685NES.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1685nes.txt".
           SELECT CHK ASSIGN TO "pb1685nes.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SF.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-K    PIC 99    VALUE 5.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-F    PIC 9V99  VALUE 2.75.
       REPORT SECTION.
       RD  R-SF CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9.99 SOURCE WS-F.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 99.99 SUM WS-F.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SF.
           GENERATE DET.
           GENERATE DET.
           TERMINATE R-SF.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
