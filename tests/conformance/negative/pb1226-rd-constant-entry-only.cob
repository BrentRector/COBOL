      *> reject-at: 2002 2014 2023
      *> kb/Work PB1226/PB1287 - ISO 13.8.4: "An RD entry shall be followed by one
      *> or more report group description entries."
      *>   cite.py: OK  13.8.4  (Report description entry)
      *> 13.8.2 admits a constant entry (13.10, COBOL-2002) among the entries
      *> after an RD, but 13.8.4 asks for REPORT GROUP description entries: a
      *> constant describes no report group, so an RD followed by constant
      *> entries alone is as undescribed as an RD followed by nothing.
      *> (At 85 the constant entry is itself rejected, COBOLNET0900, so the
      *> witness for this rule starts at 2002.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1226N2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP.
       01  KC CONSTANT AS 4.
       PROCEDURE DIVISION.
           DISPLAY KC.
           STOP RUN.
