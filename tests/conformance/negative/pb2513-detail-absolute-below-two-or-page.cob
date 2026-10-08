      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2513 - ISO 13.18.35.3 SR6 c): "Any absolute report lines shall be defined in such a way that no
      *> line appears above the upper limit or below the lower limit allowed for the report group."
      *>   cite.py: OK  13.18.35.3 6) c)  (Syntax rules)
      *> ISO 13.18.57.4 GR7 d) 3.: "If the body group is a detail, the upper limit is the line following the last
      *> line of the lowest-level control heading that has an OR PAGE phrase." Both CH-Y and CH-M have the phrase,
      *> so a page advance prints CH-Y on line 2 and CH-M (LINE PLUS 1) on line 3, and the detail's upper limit is
      *> line 4: the detail written LINE 3 lies above it. A build that placed CH-M's first line at FIRST DETAIL took
      *> the limit as 3 and compiled this; the detail then overprinted the reprinted CH-M on page 2.
      *>   cite.py: OK  13.18.57.4 7) d)  (General rules)
      *> No edition changes the rule, so all four reject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2513N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB2513N2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS R.
       WORKING-STORAGE SECTION.
       01  YR      PIC 9     VALUE 1.
       01  MO      PIC 9     VALUE 1.
       REPORT SECTION.
       RD  R CONTROLS ARE YR MO
           PAGE LIMIT IS 8 LINES HEADING 1 FIRST DETAIL 2.
       01  CH-Y TYPE CONTROL HEADING FOR YR OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "Y=".
       01  CH-M TYPE CONTROL HEADING FOR MO OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "M=".
       01  D1 TYPE DE LINE 3.
           03  COLUMN 6 PIC X VALUE "D".
       PROCEDURE DIVISION.
           STOP RUN.
