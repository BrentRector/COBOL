      *>--- conformance-case
      *> id: pb483_control_final_first
      *> rules: [SR-13.14.3-2, GR-13.18.16.4-2, FMT-13.18.16.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.16.2 (CONTROL clause general format, rendered from the PDF): the second operand form is
      *> FINAL [ data-name-1 ] ... - FINAL is written ONCE, FIRST, and the ellipsis applies only to the bracket
      *> enclosing data-name-1 (5.2.7: "the ellipsis applies to the portion of the format between the
      *> determined pair of delimiters").  13.18.16.4 GR2: "FINAL, if specified, is associated with the highest
      *> level in the hierarchy."  13.14.3 SR2: "The clauses that follow report-name-1 may appear in any
      *> order", and 13.18.39.3 SR4: "The HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, and FOOTING
      *> phrases may be written in any order" - an ORDER licence, exercised below (CONTROL after PAGE; LAST
      *> DETAIL before HEADING before FIRST DETAIL), which kb/Work PB483's once-only screens must not refuse.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  13.18.16.4 GR3: the first GENERATE saves WS-K (1) as the prior
      *> control; the second finds it unchanged; the third finds 2, so "control break processing for that level
      *> and any lower levels is performed" - WS-K is the minor level, FINAL the major (GR1/GR2), so ONLY the
      *> CF WS-K group is produced.  14.9.46.4 GR3 b): at TERMINATE "Each control footing is printed, if
      *> defined, beginning with the minor control footing ... as though a control break has been sensed in
      *> the most major control data item" - CF WS-K, then CF FINAL.  Each USE BEFORE REPORTING procedure
      *> DISPLAYs its group's name just before the group is produced, so the three lines are, in order:
      *>   BEFORE CF WS-K / BEFORE CF WS-K / BEFORE CF FINAL
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB483CFF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb483cff.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-K PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1
           PAGE LIMIT IS 30 LINES LAST DETAIL 25 HEADING 1
           FIRST DETAIL 3
           CONTROLS ARE FINAL WS-K.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE WS-K.
       01 CF-K TYPE CF WS-K LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "CF-K".
       01 CF-F TYPE CF FINAL LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "CF-F".
       PROCEDURE DIVISION.
       DECLARATIVES.
       BK-SEC SECTION.
           USE BEFORE REPORTING CF-K.
       BK-P.
           DISPLAY "BEFORE CF WS-K".
       BF-SEC SECTION.
           USE BEFORE REPORTING CF-F.
       BF-P.
           DISPLAY "BEFORE CF FINAL".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           GENERATE DET-A.
           MOVE 2 TO WS-K.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
