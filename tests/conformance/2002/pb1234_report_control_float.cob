      *> kb/Work PB1234 - a floating-point CONTROL operand. The report
      *> engine saves each control item's prior value as a string key,
      *> and a floating-point item had no restore arm, so the program
      *> compiled and then died at activation with a run-time
      *> "not implemented" for conforming source.
      *>
      *> No syntax rule of §13.18.16.3 excludes a floating-point
      *> data-name-1.
      *> "For each data-name-1 an internal data item, known as a prior
      *> control, is implicitly defined, having the same data
      *> description as the corresponding data item"
      *>   cite.py: OK  §13.18.16.4 3)  (General rules)
      *> "the control data items are restored from the prior controls
      *> to their new current values"
      *>   cite.py: OK  §13.18.16.4 4) a)  (General rules)
      *> GR4 a) stores the prior values into the control items while
      *> the control footings print, then restores the new current
      *> values - a same-usage copy each way, so the restored value is
      *> exactly the one the program held.
      *>
      *> DERIVATION. CN (FLOAT-LONG) is the minor control, CS
      *> (FLOAT-SHORT) the major one.
      *> G1: CN = 1, CS = 7 - first GENERATE, no break; DET "05".
      *> G2: unchanged; DET "05".
      *> G3: CN = 1 / 3 (a value binary64 cannot hold exactly): a
      *>     break on CN. CF-N prints with CN's PRIOR value 1 and the
      *>     sum of the two WS-K addends: "CN=1.00 S=010"; then DET.
      *>     After the GENERATE, CN holds exactly its new value again:
      *>     "CN EXACT" (the value is compared with the same quotient
      *>     computed into WS-T).
      *> G4: CS = 8 (a float-short change): a MAJOR break, so CF-N
      *>     prints first (CN = 1/3, "CN=0.33 S=005") and then CF-S
      *>     ("CS=7 S=015"); then DET. CS is 8 again afterwards.
      *> TERMINATE: CF-N "CN=0.33 S=005", CF-S "CS=8 S=005".
      *> Only non-blank lines are displayed, with their line number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1234F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1234f.txt".
           SELECT CHK ASSIGN TO "pb1234f.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  CN      USAGE FLOAT-LONG VALUE 1.
       01  CS      USAGE FLOAT-SHORT VALUE 7.
       01  WS-T    USAGE FLOAT-LONG VALUE 0.
       01  WS-K    PIC 99 VALUE 5.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE FINAL CS CN.
       01  DE-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 99 SOURCE WS-K.
       01  CF-N TYPE CF CN LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "CN=".
           03  COLUMN 4 PIC 9.99 SOURCE CN.
           03  COLUMN 9 PIC X(2) VALUE "S=".
           03  COLUMN 11 PIC 999 SUM WS-K.
       01  CF-S TYPE CF CS LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "CS=".
           03  COLUMN 4 PIC 9 SOURCE CS.
           03  COLUMN 9 PIC X(2) VALUE "S=".
           03  COLUMN 11 PIC 999 SUM WS-K.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           GENERATE DE-1.
           COMPUTE CN = 1 / 3.
           COMPUTE WS-T = 1 / 3.
           GENERATE DE-1.
           IF CN = WS-T
               DISPLAY "CN EXACT"
           ELSE
               DISPLAY "CN CHANGED"
           END-IF.
           MOVE 8 TO CS.
           GENERATE DE-1.
           IF CS = 8
               DISPLAY "CS EXACT"
           ELSE
               DISPLAY "CS CHANGED"
           END-IF.
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
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:13) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
