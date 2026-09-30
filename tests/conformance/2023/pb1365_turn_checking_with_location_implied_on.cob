      *> ISO/IEC 1989:2023 7.3.25.2 general format, printed diagram (PDF page 115, rendered): CHECKING { ON [ WITH LOCATION ] | OFF } with
      *> ONLY OFF and LOCATION underlined - ON and WITH are optional words (5.2.3), so the ON alternative may be omitted; 7.3.25.4 GR6:
      *> "If the ON phrase is specified or implied, checking ... is enabled" (cite.py --check 7.3.25.4 "If the ON phrase is specified or
      *> implied" -> OK 6)). kb/Work PB1365. Fixed form.
      *> With checking ON the out-of-range subscript (5 of 3) sets EC-BOUND-SUBSCRIPT (8.4.2.3.4 GR2, Table 13: fatal) and the
      *> declarative runs; with checking off nothing is named. The expected output is therefore the handler line, then AFTER.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1365OK2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE T (IDX) TO R.
           DISPLAY "AFTER".
           STOP RUN.
