      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1059 - ISO 13.18.39.3 SR3: "The HEADING, FIRST DETAIL, LAST
      *> CONTROL HEADING, LAST DETAIL, or FOOTING phrase may be specified only
      *> if integer-1 is specified."
      *>   cite.py: OK  13.18.39.3 3)  (Syntax rules)
      *> `PAGE 80 COLUMNS` is a legal PAGE clause (13.18.39.2: integer-2 alone
      *> is the second shape of the brace group, SR2), and `HEADING 2` is a
      *> legal phrase - but together they write a phrase with no integer-1,
      *> the page limit the phrase subdivides. The rule never had a screen
      *> while integer-1 was grammatically mandatory.
      *> No edition changes the rule (no introducedIn), so all four reject,
      *> with COBOLNET2709.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1059N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1059N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE 80 COLUMNS HEADING 2.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           STOP RUN.
