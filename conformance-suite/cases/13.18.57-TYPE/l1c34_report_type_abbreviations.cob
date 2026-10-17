      *>--- conformance-case
      *> id: l1c34_report_type_abbreviations
      *> rules: [SR-13.18.57.3-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.57.3 SR9 — RH/PH/CH/DE/CF/PF/RF abbreviate the
      *>   seven types
      *> "RH is an abbreviation for REPORT HEADING. PH is an
      *>   abbreviation for
      *> PAGE HEADING. CH is an abbreviation for CONTROL HEADING. DE
      *>   is an
      *> abbreviation for DETAIL. CF is an abbreviation for CONTROL
      *>   FOOTING.
      *> PF is an abbreviation for PAGE FOOTING. RF is an abbreviation
      *>   for
      *> REPORT FOOTING."
      *> cite.py --check 13.18.57.3 "RH is an abbreviation for REPORT
      *>   HEADING"
      *>   -> OK §13.18.57.3 9) (Syntax rules)
      *> cite.py --check 13.18.57.3 "CF is an abbreviation for CONTROL
      *>   FOOTING"
      *>   -> OK §13.18.57.3 9) (Syntax rules)
      *> Every report group below is typed ONLY by its abbreviation,
      *>   so the
      *> program is legal only if each abbreviation is accepted, and
      *>   the ORDER
      *> in which the groups are presented is fixed by §13.18.57.4 GR6
      *>   for the
      *> FULL type each abbreviation stands for. A USE BEFORE REPORTING
      *> procedure per group (§14.9.49.3 SR9) DISPLAYs the group's
      *>   name at each
      *> presentation, so an abbreviation mapped to the wrong type (CH
      *>   read as
      *> CF, PF as PH, ...) reorders or drops lines.
      *> cite.py --check 13.18.57.4 "The report heading, if defined,
      *>   is printed
      *>   as the first report group in the report" -> OK §13.18.57.4
      *>     6) a)
      *> cite.py --check 13.18.57.4 "in order of controls from the
      *>   lowest up to
      *>   the level of the control break" -> OK §13.18.57.4 6) e) 1.
      *> cite.py --check 13.18.57.4 "the page footing on the last page
      *>   is
      *>   immediately followed by the report footing" -> OK
      *>     §13.18.57.4 6) f) 2.
      *>   (cite.py labels this unnumbered sentence as item f) 2.; it
      *>     is the
      *>   closing sentence of GR6 f) - a known list-item mislabel,
      *>     PB1554.)
      *> DERIVATION (WS-K is the one control; DE is generated with K=1
      *>   then K=2):
      *>  GENERATE #1 (first after INITIATE): RH first (GR6 a); PH
      *>    immediately
      *>   before the first body group (GR6 b); CH on the first GENERATE
      *>   (GR6 c 1.); then the DETAIL (GR6 d)       -> RH, PH, CH, DE
      *>  GENERATE #2: K changed, a control break: CF before any CH/DE
      *>   (GR6 e 1.), then CH (GR6 c 2.), then DE    -> CF, CH, DE
      *>  TERMINATE: CF (GR6 e 2.); the page footing is the last group
      *>    on the
      *>   page (GR6 f) and is immediately followed by the report
      *>     footing, the
      *>   very last group (GR6 f, g)                  -> CF, PF, RF
      *> Nothing is printed on a second page: 10 lines fit lines 1-26
      *>   of page 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "L1C34C.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-AB.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R-AB CONTROL IS WS-K
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 20 FOOTING 24.
       01  G-RH TYPE RH LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "RH".
       01  G-PH TYPE PH LINE 3.
           02  COLUMN 1 PIC X(2) VALUE "PH".
       01  G-CH TYPE CH WS-K LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CH".
       01  G-DE TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
       01  G-CF TYPE CF WS-K LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "CF".
       01  G-PF TYPE PF LINE 25.
           02  COLUMN 1 PIC X(2) VALUE "PF".
       01  G-RF TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       S-RH SECTION.
           USE BEFORE REPORTING G-RH.
       P-RH.
           DISPLAY "RH".
       S-PH SECTION.
           USE BEFORE REPORTING G-PH.
       P-PH.
           DISPLAY "PH".
       S-CH SECTION.
           USE BEFORE REPORTING G-CH.
       P-CH.
           DISPLAY "CH".
       S-DE SECTION.
           USE BEFORE REPORTING G-DE.
       P-DE.
           DISPLAY "DE".
       S-CF SECTION.
           USE BEFORE REPORTING G-CF.
       P-CF.
           DISPLAY "CF".
       S-PF SECTION.
           USE BEFORE REPORTING G-PF.
       P-PF.
           DISPLAY "PF".
       S-RF SECTION.
           USE BEFORE REPORTING G-RF.
       P-RF.
           DISPLAY "RF".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-AB.
           GENERATE G-DE.
           MOVE 2 TO WS-K.
           GENERATE G-DE.
           TERMINATE R-AB.
           CLOSE RPT.
           STOP RUN.
