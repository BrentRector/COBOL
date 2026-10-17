      *>--- conformance-case
      *> id: l1c20_page_clause_defaults
      *> rules: [GR-13.18.39.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.39.4 GR3 — the PAGE clause's default HEADING,
      *> FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL and FOOTING
      *> integers, every arm, observed through page advances.
      *>
      *> THE RULES.
      *> §13.18.39.4 GR3: "If integer-1 is specified and any of the
      *>   following phrases is omitted, a default value for the
      *>   associated integer is supplied as follows:
      *>   a) If HEADING is omitted, integer-3 will be 1.
      *>   OK  §13.18.39.4 3) a)  (General rules)
      *>   b) If FIRST DETAIL is omitted, integer-4 will be equal to
      *>   integer-3.
      *>   OK  §13.18.39.4 3) b)  (General rules)
      *>   c) If LAST CONTROL HEADING is omitted, integer-5 will be
      *>   equal to integer-6, if LAST DETAIL is specified, or equal
      *>   to integer-7, if FOOTING is specified, or otherwise equal to
      *>   the page limit given by integer-1.
      *>   OK  §13.18.39.4 3) c)  (General rules)
      *>   d) If LAST DETAIL is omitted, integer-6 will be equal to
      *>   integer-7, if FOOTING is specified, or otherwise equal to
      *>   the page limit given by integer-1.
      *>   OK  §13.18.39.4 3) d)  (General rules)
      *>   e) If FOOTING is omitted, integer-7 will be equal to
      *>   integer-6, if LAST DETAIL is specified, or otherwise equal
      *>   to the page limit given by integer-1."
      *>   OK  §13.18.39.4 3) d)  (General rules)  [cite.py files the
      *>   flattened e) text under the d) label; the text is e)]
      *> The observation machinery:
      *> §13.18.35.4 GR4 c): relative page fit - the trial sum is
      *>   LINE-COUNTER plus each LINE's integer-2; "If the trial sum
      *>   is less than or equal to the lower limit for the report
      *>   group, the page fit is declared successful", otherwise a
      *>   page advance precedes the group.
      *>   OK  §13.18.35.4 4)  (General rules)
      *> §13.18.57.4 GR8: lower limits d) "for a control heading is the
      *>   line given by the LAST CONTROL HEADING integer" e) "for a
      *>   detail is the line given by the LAST DETAIL integer" f)
      *>   "for a control footing is the line given by the FOOTING
      *>   integer".
      *>   OK  §13.18.57.4 8) d)  /  8) e)  /  8) f)  (General rules)
      *> §13.18.35.4 GR5 b) 3.: "if that report group is the first body
      *>   group to be printed on the current page, the line number is
      *>   given by the FIRST DETAIL integer"; otherwise LINE-COUNTER
      *>   plus integer-2.
      *>   OK  §13.18.35.4 5) b) 3.  (General rules)
      *> §8.4.3.15.4 GR2: PAGE-COUNTER starts at 1 and "its value is
      *>   updated by 1 during each page advance"; GR4: "The value of
      *>   LINE-COUNTER after the printing of a report group is the
      *>   same as the line number of the last line printed".
      *>   OK  §8.4.3.15.4 2)  /  4)  (General rules)
      *> §14.9.16.4 GR4 c): the first GENERATE prints "each control
      *>   heading ... in order from major to minor" before the
      *>   detail; GR5 a): at a later control break "each control
      *>   footing and control heading is printed, if defined, up to
      *>   the level of the control break".
      *>   OK  §14.9.16.4 4) a)  (cite.py label; the text is 4) c))
      *>   OK  §14.9.16.4 5) a)  (General rules)
      *> Every group here is one LINE PLUS 1 line. Each output line is
      *> "<report> <PAGE-COUNTER>/<LINE-COUNTER>" after one GENERATE.
      *>
      *> DERIVATION.
      *> R1  PAGE LIMIT 10 FOOTING 4 (DE only). a) HEADING 1, b) FIRST
      *>     DETAIL 1, d) LAST DETAIL = FOOTING 4. Details on lines
      *>     1..4; the 5th: trial 5 > 4 -> advance, line 1 of page 2.
      *>     R1 1/01 1/02 1/03 1/04 2/01 2/02.
      *> R2  PAGE LIMIT 10 LAST DETAIL 4 (CF K2 + DE). e) FOOTING =
      *>     LAST DETAIL 4. Details 1..4; K2 changes: CF trial 5 > 4
      *>     -> advance, CF on line 1 of page 2, DE on line 2.
      *>     R2 1/01 1/02 1/03 1/04 2/02.  (FOOTING 10 would give CF on
      *>     line 5 of page 1, DE on page 2 line 1: 2/01.)
      *> R3  PAGE LIMIT 10 LAST DETAIL 6 FOOTING 8 (CH K3 + DE).
      *>     c) LAST CH = LAST DETAIL 6. First GENERATE: CH line 1,
      *>     DE line 2; details 3..6; K3 changes: CH trial 7 > 6 ->
      *>     advance, CH line 1, DE line 2 of page 2.
      *>     R3 1/02 1/03 1/04 1/05 1/06 2/02.  (8 would give 2/01.)
      *> R4  PAGE LIMIT 10 FOOTING 5 (CH K4 + DE). c) LAST CH =
      *>     FOOTING 5 (d) LAST DETAIL 5 too). CH 1, DE 2..5; K4
      *>     changes: CH trial 6 > 5 -> advance.
      *>     R4 1/02 1/03 1/04 1/05 2/02.  (10 would give 2/01.)
      *> R5  PAGE LIMIT 6 (CH K5 + DE). c) LAST CH = 6, d) LAST
      *>     DETAIL = 6. CH 1, DE 2..5; K5 changes: CH trial 6 <= 6
      *>     fits on line 6; DE trial 7 > 6 -> advance, page 2 line 1;
      *>     five more details on lines 2..6 (6 fits: d)); the next:
      *>     trial 7 > 6 -> page 3 line 1.
      *>     R5 1/02 1/03 1/04 1/05 2/01 2/02 2/03 2/04 2/05 2/06 3/01.
      *> R6  PAGE LIMIT 6 (CF K6 + DE). e) FOOTING = 6. DE 1..5; K6
      *>     changes: CF trial 6 <= 6 fits on line 6; DE trial 7 > 6
      *>     -> advance, page 2 line 1.
      *>     R6 1/01 1/02 1/03 1/04 1/05 2/01.  (a FOOTING below 6
      *>     would advance for the CF instead: 2/02.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "L1C20K1.RPT".
           SELECT P2 ASSIGN TO "L1C20K2.RPT".
           SELECT P3 ASSIGN TO "L1C20K3.RPT".
           SELECT P4 ASSIGN TO "L1C20K4.RPT".
           SELECT P5 ASSIGN TO "L1C20K5.RPT".
           SELECT P6 ASSIGN TO "L1C20K6.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD P1 REPORT IS R1.
       FD P2 REPORT IS R2.
       FD P3 REPORT IS R3.
       FD P4 REPORT IS R4.
       FD P5 REPORT IS R5.
       FD P6 REPORT IS R6.
       WORKING-STORAGE SECTION.
       01 K2 PIC 9 VALUE 1.
       01 K3 PIC 9 VALUE 1.
       01 K4 PIC 9 VALUE 1.
       01 K5 PIC 9 VALUE 1.
       01 K6 PIC 9 VALUE 1.
       01 PC PIC 9.
       01 LC PIC 99.
       REPORT SECTION.
       RD R1 PAGE LIMIT 10 LINES FOOTING 4.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R2 PAGE LIMIT 10 LINES LAST DETAIL 4
             CONTROLS ARE K2.
       01 D2 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       01 F2 TYPE CF K2 LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "F".
       RD R3 PAGE LIMIT 10 LINES LAST DETAIL 6 FOOTING 8
             CONTROLS ARE K3.
       01 H3 TYPE CH K3 LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "H".
       01 D3 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R4 PAGE LIMIT 10 LINES FOOTING 5
             CONTROLS ARE K4.
       01 H4 TYPE CH K4 LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "H".
       01 D4 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R5 PAGE LIMIT 6 LINES
             CONTROLS ARE K5.
       01 H5 TYPE CH K5 LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "H".
       01 D5 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R6 PAGE LIMIT 6 LINES
             CONTROLS ARE K6.
       01 D6 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       01 F6 TYPE CF K6 LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "F".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT P1 P2 P3 P4 P5 P6.
           INITIATE R1 R2 R3 R4 R5 R6.
           PERFORM G1 6 TIMES.
           PERFORM G2 4 TIMES.
           MOVE 2 TO K2.
           PERFORM G2.
           PERFORM G3 5 TIMES.
           MOVE 2 TO K3.
           PERFORM G3.
           PERFORM G4 4 TIMES.
           MOVE 2 TO K4.
           PERFORM G4.
           PERFORM G5 4 TIMES.
           MOVE 2 TO K5.
           PERFORM G5 7 TIMES.
           PERFORM G6 5 TIMES.
           MOVE 2 TO K6.
           PERFORM G6.
           TERMINATE R1 R2 R3 R4 R5 R6.
           CLOSE P1 P2 P3 P4 P5 P6.
           STOP RUN.
       G1.
           GENERATE D1.
           MOVE PAGE-COUNTER OF R1 TO PC.
           MOVE LINE-COUNTER OF R1 TO LC.
           DISPLAY "R1 " PC "/" LC.
       G2.
           GENERATE D2.
           MOVE PAGE-COUNTER OF R2 TO PC.
           MOVE LINE-COUNTER OF R2 TO LC.
           DISPLAY "R2 " PC "/" LC.
       G3.
           GENERATE D3.
           MOVE PAGE-COUNTER OF R3 TO PC.
           MOVE LINE-COUNTER OF R3 TO LC.
           DISPLAY "R3 " PC "/" LC.
       G4.
           GENERATE D4.
           MOVE PAGE-COUNTER OF R4 TO PC.
           MOVE LINE-COUNTER OF R4 TO LC.
           DISPLAY "R4 " PC "/" LC.
       G5.
           GENERATE D5.
           MOVE PAGE-COUNTER OF R5 TO PC.
           MOVE LINE-COUNTER OF R5 TO LC.
           DISPLAY "R5 " PC "/" LC.
       G6.
           GENERATE D6.
           MOVE PAGE-COUNTER OF R6 TO PC.
           MOVE LINE-COUNTER OF R6 TO LC.
           DISPLAY "R6 " PC "/" LC.
