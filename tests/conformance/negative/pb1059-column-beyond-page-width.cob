      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1059 - ISO 13.18.14.3 SR6: "Neither integer-1 nor integer-2
      *> shall exceed the page width."
      *>   cite.py: OK  13.18.14.3 6)  (Syntax rules)
      *> 13.18.39.4 GR2 b): integer-2 of the PAGE clause is the page width.
      *> The page width here is 20 (`PAGE LIMIT IS 10 LINES 20 COLUMNS`) and the
      *> COLUMN clause's integer-1 is 25, so the program breaks SR6 - at COMPILE
      *> time, because the operand is a written integer. (The run-time twin for
      *> a line whose FINAL column passes the width is EC-REPORT-PAGE-WIDTH,
      *> 13.18.14.4 GR5.)
      *> No edition changes the rule, so all four reject, with COBOLNET2710.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1059N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1059N2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES 20 COLUMNS.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 25 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           STOP RUN.
