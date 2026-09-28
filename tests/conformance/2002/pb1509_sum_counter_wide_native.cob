      *> kb/Work PB1509 + PB1560 + PB1666 - a report SUM counter whose
      *> PICTURE has more than 18 digits, under ARITHMETIC IS NATIVE.
      *> The report engine carried every sum counter in a 64-bit long:
      *> the addend delegate was `() => (long)(addend)`, so a 20-digit
      *> addend wrapped modulo 2^64 (PB1509/PB1560), and the counter's
      *> capacity was clamped to the 18 digits a long holds, so a total
      *> past 9.2E18 was a FALSE size error that printed spaces (PB1666).
      *>
      *> "The number of decimal digits in the sum counter, both
      *> integral and fractional, is derived from the corresponding
      *> number of digits, excluding insertion editing characters, in
      *> the PICTURE clause of the entry containing the SUM clause."
      *>   cite.py: OK  §13.18.54.4 1)  (General rules)
      *> "The adding is consistent with the general rules of the ADD
      *> statement with the ON SIZE ERROR phrase"
      *>   cite.py: OK  §13.18.54.4 3)  (General rules)
      *> "If the associated size error indicator is set, an
      *> EC-REPORT-SUM-SIZE exception condition is set to exist and the
      *> printable item is filled with spaces."
      *>   cite.py: OK  §13.18.54.4 4)  (General rules)
      *> "It is permissible for procedure division statements to alter
      *> the content of sum counters."
      *>   cite.py: OK  §13.18.54.4 12)  (General rules)
      *> "the number of digit positions described by character-string-1
      *> shall range from 1 through 31."
      *>   cite.py: OK  §13.18.40.3 14)  (Syntax rules)
      *> "Native arithmetic is in effect when the ARITHMETIC IS NATIVE
      *> clause is specified in the OPTIONS paragraph"
      *>   cite.py: OK  §8.8.1.3  (Native arithmetic)
      *>
      *> DERIVATION (two GENERATEs, then TERMINATE prints CF FINAL):
      *> S1  PIC 9(20) SUM WS-A WS-A - four additions of 6E18 into a
      *>     20-digit counter: 24000000000000000000 (fits 20 digits).
      *> S2  PIC 9(20) SUM WS-B - 2 x 12345678901234567890 =
      *>     24691357802469135780 (fits 20 digits).
      *> S3  PIC 9(29)V99 SUM WS-C - 2 x 1234567890123456789012345678.91
      *>     = 2469135780246913578024691357.82, printed as its 31 digit
      *>     positions with the point implied:
      *>     0246913578024691357802469135782.
      *> S4  PIC 9(20) SUM WS-D - the first addition stores 6E19; the
      *>     second makes 1.2E20, 21 digits: a size error, so the
      *>     indicator is set and the printable item is SPACES (GR4).
      *> CTR PIC 9(22) SUM WS-B, named: after the two GENERATEs it is
      *>     24691357802469135780; ADD 1E21 TO CTR (GR12) makes
      *>     1024691357802469135780, which DISPLAY shows and the CF
      *>     prints.
      *> Only non-blank lines are displayed, with their line number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1509N.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1509n.txt".
           SELECT CHK ASSIGN TO "pb1509n.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 9(19) VALUE 6000000000000000000.
       01  WS-B    PIC 9(20) VALUE 12345678901234567890.
       01  WS-C    PIC 9(28)V99
                   VALUE 1234567890123456789012345678.91.
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
               03  COLUMN 1  PIC 9(20) SUM WS-B.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9(29)V99 SUM WS-C.
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
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:31) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
