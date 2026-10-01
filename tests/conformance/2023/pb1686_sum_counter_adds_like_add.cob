      *> PB1686 - ISO 13.18.54.4 GR3: "The adding is consistent with the general
      *>   rules of the ADD statement ..." and GR9 sums a group's addends together.
      *>   ADD forms the exact sum and only THEN stores it at the receiver's scale,
      *>   so an addend finer than the counter must not be cut to the counter's
      *>   scale one addend at a time.
      *>   Two GENERATEs; the items hold (F, G, H) = (1.000, 0.006, 0.006) and
      *>   then (-0.005, 0.006, 0.006).  Each counter is PIC 9V99.
      *>   A = SUM F:      1.000 -> 1.00; then 1.00 + (-0.005) = 0.995 -> 0.99
      *>                   (truncation; each addend cut first would keep 1.00).
      *>   B = SUM G H:    0.006 + 0.006 = 0.012 -> 0.01; then 0.01 + 0.012 =
      *>                   0.022 -> 0.02 (each addend cut first would keep 0.00).
      *>   C = SUM F ROUNDED: 1.000 -> 1.00; then 0.995 rounds to 1.00
      *>                   (nearest away from zero).  The SUM clause's own rounded-phrase
      *>                   (13.18.54.2) is the mode of the ADD-consistent store into the
      *>                   counter (13.18.54.4 GR3) - a determination, 13.18.54.4 GR4
      *>                   speaks of the SOURCE clause's phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1686SCA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1686sca.txt".
           SELECT CHK ASSIGN TO "pb1686sca.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SF.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-F    PIC S9V999 VALUE 0.
       01  WS-G    PIC S9V999 VALUE 0.006.
       01  WS-H    PIC S9V999 VALUE 0.006.
       REPORT SECTION.
       RD  R-SF CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC -9.999 SOURCE WS-F.
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9.99 SUM WS-F.
               03  COLUMN 8 PIC 9.99 SUM WS-G WS-H.
               03  COLUMN 15 PIC 9.99 SUM WS-F ROUNDED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SF.
           MOVE 1.000 TO WS-F.
           GENERATE DET.
           MOVE -0.005 TO WS-F.
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
