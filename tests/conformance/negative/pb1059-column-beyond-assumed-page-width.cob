      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1059 - ISO 13.18.39.4 GR5: "If integer-2 is omitted, a value
      *> of 999 is assumed for the page width."
      *>   cite.py: OK  13.18.39.4 5)  (General rules)
      *> 13.18.14.3 SR6: "Neither integer-1 nor integer-2 shall exceed the page
      *> width."
      *>   cite.py: OK  13.18.14.3 6)  (Syntax rules)
      *> This PAGE clause omits integer-2, so the page width is the assumed 999
      *> and the COLUMN clause's integer-1 of 1000 exceeds it. The width was a
      *> run-time constant with no compile-time reader, so the program
      *> compiled.
      *> No edition changes the rule, so all four reject, with COBOLNET2710.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1059N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1059N3.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1000 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           STOP RUN.
