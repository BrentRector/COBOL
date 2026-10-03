      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1298 - ISO 13.18.57.3 SR12: "PAGE HEADING and PAGE FOOTING and
      *> the OR PAGE phrase are allowed only if a PAGE clause that defines the
      *> page limit is specified in the report description entry."
      *>   cite.py: OK  13.18.57.3 12)  (Syntax rules)
      *> The phrase is legal syntax (13.18.57.2) - the RD below has no PAGE
      *> clause, so no page limit, so the OR PAGE phrase is refused. The PH and PF
      *> legs of the rule were already screened; the OR PAGE leg had no phrase to
      *> screen.
      *> No edition changes the rule, so all four reject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1298N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1298N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  CX PIC X VALUE "A".
       REPORT SECTION.
       RD  RP CONTROL IS CX.
       01  CH1 TYPE CH CX OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "HHH".
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           STOP RUN.
