      *> kb/Work PB1509 - the SUM arm of ARITHMETIC IS STANDARD-DECIMAL.
      *> "If the STANDARD-DECIMAL phrase is specified, the techniques
      *> used in handling arithmetic expressions, arithmetic statements,
      *> the SUM clause, and integer and numeric functions shall be as
      *> described for standard-decimal arithmetic"
      *>   cite.py: OK  §11.9.5.2 3)  (General rules)
      *> The report engine accumulated every sum counter through a C#
      *> `(long)` cast, so a 20-digit addend wrapped modulo 2^64 under
      *> STANDARD-DECIMAL exactly as under NATIVE.
      *> "The number of decimal digits in the sum counter, both
      *> integral and fractional, is derived from the corresponding
      *> number of digits, excluding insertion editing characters, in
      *> the PICTURE clause of the entry containing the SUM clause."
      *>   cite.py: OK  §13.18.54.4 1)  (General rules)
      *> "The adding is consistent with the general rules of the ADD
      *> statement with the ON SIZE ERROR phrase or, in the case of an
      *> arithmetic expression, the COMPUTE statement with the ON SIZE
      *> ERROR phrase."
      *>   cite.py: OK  §13.18.54.4 3)  (General rules)
      *> Every value below is exact in a 34-digit standard-decimal
      *> intermediate (§8.8.1.5.2), so no rounding intervenes.
      *>
      *> DERIVATION (two GENERATEs, then TERMINATE prints CF FINAL):
      *> S1  PIC 9(20) SUM WS-A WS-A - four additions of 6E18:
      *>     24000000000000000000.
      *> S2  PIC 9(21) SUM WS-B * 3 - an arithmetic-expression addend,
      *>     twice 3 x 12345678901234567890 = 74074073407407407340,
      *>     printed in 21 digit positions: 074074073407407407340.
      *> S3  PIC 9(20) SUM WS-D - 6E19, then 1.2E20 is 21 digits: a
      *>     size error, so the printable item is SPACES (GR4).
      *> CTR PIC 9(22) SUM WS-B, named: 24691357802469135780 after the
      *>     two GENERATEs; ADD 1E21 TO CTR (GR12) gives
      *>     1024691357802469135780.
      *> Only non-blank lines are displayed, with their line number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1509S.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1509s.txt".
           SELECT CHK ASSIGN TO "pb1509s.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 9(19) VALUE 6000000000000000000.
       01  WS-B    PIC 9(20) VALUE 12345678901234567890.
       01  WS-D    PIC 9(20) VALUE 60000000000000000000.
       01  WS-W    PIC 9(22) VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL.
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CF-F TYPE CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9(20) SUM WS-A WS-A.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9(21) SUM WS-B * 3.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9(20) SUM WS-D.
               03  COLUMN 22 PIC X VALUE "|".
           02  LINE PLUS 1.
               03  CTR COLUMN 1 PIC 9(22) SUM WS-B.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           GENERATE DE-1.
           ADD 1000000000000000000000 TO CTR.
           MOVE CTR TO WS-W.
           DISPLAY "CTR=" WS-W.
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
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:22) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
