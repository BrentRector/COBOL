      *> kb/Work PB1296 - a sum counter's capacity is the digit count of
      *> its entry's PICTURE, category by category, and an addition past
      *> it is a size error whose printable item is filled with spaces.
      *>
      *> "The number of decimal digits in the sum counter, both integral
      *> and fractional, is derived from the corresponding number of
      *> digits, excluding insertion editing characters, in the PICTURE
      *> clause of the entry containing the SUM clause."
      *>   cite.py: OK  §13.18.54.4 1)  (General rules)
      *> "Each addition is tested for size error; if a size error
      *> occurs, the EC-REPORT-SUM-SIZE exception condition is set to
      *> exist."
      *>   cite.py: OK  §13.18.54.4 3)  (General rules)
      *> "If the associated size error indicator is set, an
      *> EC-REPORT-SUM-SIZE exception condition is set to exist and the
      *> printable item is filled with spaces."
      *>   cite.py: OK  §13.18.54.4 4)  (General rules)
      *>
      *> DERIVATION. Two GENERATEs each add WS-K = 60, total 120.
      *> ZZ9, Z(3) and $$$9 are three-digit counters (every digit
      *> position, not only the 9s): 120, 120, $120. 9,999 is four
      *> digits (the comma is an insertion character): 0,120. X(4) is a
      *> four-digit counter moved as an unsigned integer: 0120. XXBX is
      *> three (B is an insertion character): 120 edited to "12 0".
      *> 99 is two digits: 60 + 60 = 120 is a size error, so the item
      *> is spaces. Fails with spaces in the first four items if the
      *> capacity counts only the 9 symbols (ZZ9 read as one digit).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1296T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1296t.txt".
           SELECT CHK ASSIGN TO "pb1296t.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 99    VALUE 60.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(45) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CF-F TYPE CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC ZZ9 SUM WS-K.
               03  COLUMN 5  PIC Z(3) SUM WS-K.
               03  COLUMN 9  PIC $$$9 SUM WS-K.
               03  COLUMN 14 PIC 9,999 SUM WS-K.
               03  COLUMN 20 PIC X(4) SUM WS-K.
               03  COLUMN 25 PIC XXBX SUM WS-K.
               03  COLUMN 30 PIC 99 SUM WS-K.
               03  COLUMN 33 PIC X VALUE "|".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:33) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
