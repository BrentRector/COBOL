      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1298 - ISO 13.18.57.2 Format 2, rendered: only the CONTROL
      *> HEADING row carries `[ OR PAGE ]`:
      *>   {CONTROL HEADING | CH} [ [ON | FOR] {data-name-1 | FINAL} [OR PAGE] ]
      *>   {CONTROL FOOTING | CF} [ [ON | FOR] {data-name-2 | FINAL} ]
      *>   cite.py: OK  13.18.57.2  (General formats)
      *> A control footing written with OR PAGE is not in the general format, so
      *> the grammar has no phrase for it (the figure note says the CONTROL FOOTING
      *> row "has the same shape but no OR PAGE phrase"): a parse error at every
      *> edition. This pins the shape from the negative side, so a grammar that
      *> shares one operand rule between the two rows cannot slip the phrase onto
      *> the footing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1298N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1298N2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  CX PIC X VALUE "A".
       REPORT SECTION.
       RD  RP CONTROL IS CX PAGE LIMIT IS 20 LINES.
       01  CF1 TYPE CF CX OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "FFF".
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
           STOP RUN.
