      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1189 - ISO 14.9.51.2 Format 1 prints the ADVANCING operand as two alternatives:
      *> { identifier-2 | integer-1 } [ LINE | LINES ]  and  { mnemonic-name-1 | PAGE }  (cite.py: OK
      *> 14.9.51.2). LINE / LINES belong to the count alternative only, so `mnemonic-name-1 LINES` is no
      *> WRITE statement - exactly as `AFTER ADVANCING PAGE LINES` is not. The word used to be dropped in
      *> silence and this program compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1031GPB1189NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           C01 IS TOP-OF-FORM.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "w1031gpb1189.dat".
       DATA DIVISION.
       FILE SECTION.
       FD PRT.
       01 PRT-REC PIC X(4).
       PROCEDURE DIVISION.
           OPEN OUTPUT PRT.
           MOVE "WXYZ" TO PRT-REC.
           WRITE PRT-REC AFTER ADVANCING TOP-OF-FORM LINES.
           CLOSE PRT.
           DISPLAY "DONE".
           STOP RUN.
