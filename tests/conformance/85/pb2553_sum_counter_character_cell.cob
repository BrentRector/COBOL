      *> kb/Work PB2553 - A SUM COUNTER KEEPS THE CHARACTERS STORED IN IT.
      *>
      *> 13.18.54.4 GR1 makes a sum counter a numeric data item, and this
      *> implementation makes it signed USAGE DISPLAY (CONFORMANCE.md
      *> A.4.11), so 8.4.3.3.3 SR1 admits a reference modifier on it and
      *> GR12 permits procedure division statements to alter it. 8.4.3.3.4
      *> GR5: the reference-modified item is "a subset of the data item
      *> referenced by identifier-1", so a character MOVEd into CF-T (2:1)
      *> is in the counter afterwards, a non-digit included, exactly as in a
      *> stored item: CF-T (1:4) reads "1X3D" (1234 is "123D" in the default
      *> trailing overpunch). 8.8.4.4.4 GR3 n): the content is not digits,
      *> so CF-T IS NUMERIC is false. The pre-2023 figurative fill (14.9.25.3
      *> SR5, removed in 2023) deposits four spaces, likewise not numeric. A
      *> numeric MOVE makes the content valid again: 56 is "005F", NUMERIC,
      *> and moves to PIC 9999 as 0056. A plain "6" stored over the sign
      *> position is a valid image of the same value that is not the
      *> canonical one; GR5 keeps it, so CF-T (1:4) reads "0056" (train
      *> 1045 review), and its value is still 56.
      *> The counter used to decode the
      *> stored characters back into its integer register (CF-T (1:4)
      *> read "013D", NUMERIC) and MOVE SPACE aborted the run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2553A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB2553A.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X     PIC 9999 VALUE 1234.
       01  WS-A     PIC X(4) VALUE SPACES.
       01  WS-D     PIC 9999 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  CF-T COLUMN 1  PIC 9999 SUM WS-X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE "X" TO CF-T (2:1).
           MOVE CF-T (1:4) TO WS-A.
           DISPLAY "A=[" WS-A "]".
           IF CF-T IS NUMERIC
               DISPLAY "A NUMERIC"
           ELSE
               DISPLAY "A NOT NUMERIC"
           END-IF.
           MOVE SPACE TO CF-T.
           MOVE CF-T (1:4) TO WS-A.
           DISPLAY "S=[" WS-A "]".
           IF CF-T IS NUMERIC
               DISPLAY "S NUMERIC"
           ELSE
               DISPLAY "S NOT NUMERIC"
           END-IF.
           MOVE 56 TO CF-T.
           MOVE CF-T (1:4) TO WS-A.
           DISPLAY "V=[" WS-A "]".
           IF CF-T IS NUMERIC
               DISPLAY "V NUMERIC"
           ELSE
               DISPLAY "V NOT NUMERIC"
           END-IF.
           MOVE CF-T TO WS-D.
           DISPLAY "D=[" WS-D "]".
           MOVE "6" TO CF-T (4:1).
           MOVE CF-T (1:4) TO WS-A.
           DISPLAY "P=[" WS-A "]".
           MOVE CF-T TO WS-D.
           DISPLAY "Q=[" WS-D "]".
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
