      *> reject-at: 85
      *> kb/Work PB1226 - the constant entry (13.10) is a COBOL-2002 introduction
      *> (the constant-entry-2002 construct), and 13.8.2 admits it in the REPORT
      *> SECTION only where the edition has it. Below 2002 the same entry that
      *> compiles at 2002+ is
      *> rejected by the edition gate (COBOLNET0900), the one gate the
      *> data-division constant entry already takes: a report-section constant
      *> rides the SAME constantEntryBody rule, so it gets the SAME gate.
      *>   cite.py: OK  13.8.2  (General format)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1226N3.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 10 LINES.
       01  KC CONSTANT AS 4.
       01  DL TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 VALUE 4.
       PROCEDURE DIVISION.
           STOP RUN.
