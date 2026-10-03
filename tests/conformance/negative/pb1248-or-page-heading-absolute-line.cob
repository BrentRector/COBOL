      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1248 - ISO 13.18.35.3 SR9: "If the current report group is a
      *> control heading with the OR PAGE phrase, all the LINE clauses in the
      *> report group description shall be relative."
      *>   cite.py: OK  13.18.35.3 9)  (Syntax rules)
      *> `TYPE CH CX OR PAGE` (13.18.57.2) is legal, and `LINE 5` is legal in a
      *> control heading without the phrase - together they break SR9. The rule
      *> had no site while the phrase had no grammar surface.
      *> No edition changes the rule, so all four reject, with COBOLNET2199 (the
      *> LINE clause's syntax-rule band).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1248N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1248N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  CX PIC X VALUE "A".
       REPORT SECTION.
       RD  RP CONTROL IS CX PAGE LIMIT IS 20 LINES.
       01  CH1 TYPE CH CX OR PAGE LINE 5.
           03  COLUMN 1 PIC X(3) VALUE "HHH".
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           STOP RUN.
