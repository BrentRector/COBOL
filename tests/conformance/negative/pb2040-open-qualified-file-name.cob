      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2040 - a file-name has no qualified format: ISO 8.4.2.2.2 prints qualified formats for
      *> data-names, condition-names, index-names and the other qualifiable names, and a file-name appears
      *> there only as the file-report-qualifier of another name. cite.py: OK 8.4.2.2.2.
      *> The operand used to reach the resolver as its glued text AOFB, so this program compiled clean,
      *> opened the unrelated file AOFB and printed DONE. Now the qualifier is refused by name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1031GPB2040OPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AOFB ASSIGN TO "w1031gpb2040.dat".
       DATA DIVISION.
       FILE SECTION.
       FD AOFB.
       01 R PIC X(4).
       PROCEDURE DIVISION.
           OPEN OUTPUT A OF B.
           MOVE "WXYZ" TO R.
           WRITE R.
           CLOSE AOFB.
           DISPLAY "DONE".
           STOP RUN.
