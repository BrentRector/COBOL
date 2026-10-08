      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2513 - ISO 13.18.35.3 SR6 c): "Any absolute report lines shall be defined in such a way that no
      *> line appears above the upper limit or below the lower limit allowed for the report group."
      *>   cite.py: OK  13.18.35.3 6) c)  (Syntax rules)
      *> ISO 13.18.57.4 GR7 d) 2. and 4.: the upper limit of a control heading at a lower level than the highest OR
      *> PAGE heading is "the line following the last line of the next higher-level control heading"; a control
      *> footing's is "the line following the last line of the lowest-level control heading with an OR PAGE
      *> phrase at the same level as the control footing, or higher".
      *>   cite.py: OK  13.18.57.4 7) d)  (General rules)
      *> PAGE LIMIT 8, HEADING 1, FIRST DETAIL 2. CH-Y (OR PAGE) is the first body group of a page, on line 2;
      *> CH-M (OR PAGE, LINE PLUS 1) follows it on line 3; so CF-M's upper limit is line 4 and CF-M LINE 3 stands
      *> above it, where the reprinted CH-M is. A build that placed every heading's first line at FIRST DETAIL took
      *> CH-M's last line as 2, the limit as 3, and compiled this with no diagnostic.
      *> No edition changes the rule, so all four reject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2513N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB2513N1.TXT".
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
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X VALUE "D".
       01  CF-M TYPE CF FOR MO LINE 3.
           03  COLUMN 1 PIC XX VALUE "TM".
       PROCEDURE DIVISION.
           STOP RUN.
