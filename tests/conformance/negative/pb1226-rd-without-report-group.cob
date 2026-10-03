      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1226/PB1287 - ISO 13.8.4: "An RD entry shall be followed by one
      *> or more report group description entries."
      *>   cite.py: OK  13.8.4  (Report description entry)
      *> 13.8.2 prints the list after a report description entry as a brace group
      *> with an ellipsis, so it is one or more at every edition. This RD is
      *> followed by NOTHING, so the program describes no report group at all;
      *> before the rule was screened it compiled clean and ran.
      *> No edition changes the rule (Annex E has no change to 13.8.4), so all
      *> four editions reject, with COBOLNET2708 (the sort-merge twin's "one or
      *> more record description entries" is COBOLNET1837).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1226N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1226N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP.
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
