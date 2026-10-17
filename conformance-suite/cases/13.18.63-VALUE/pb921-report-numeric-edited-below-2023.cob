*>--- conformance-case
*> id: pb921-report-numeric-edited-below-2023
*> rules: [SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR6 NAMES FORMAT 4 - "literals in formats 1, 2, and 4 of the VALUE clause may be numeric"
      *> - so a report-section printable item's numeric literal rides exactly the same COBOL-2023 introduction
      *> (Annex E.3.3 item 43) as its format-1 and format-2 siblings. It did not: a report entry's VALUE operands
      *> never passed through the data-division literal funnel, so this program compiled clean at --std 85 and
      *> PRINTED the COBOL-2023 edited image ` 10.00`, while `01 X PIC ZZ9.99 VALUE 10.` was refused there
      *> (kb/Work PB921 - the third arm of the one dispatch; the other two are format 1, already gated, and
      *> format 3, whose gating negative is pb921-condition-name-numeric-edited-below-2023).
      *>
      *> The report-writer vehicle itself is available at every edition this case is rejected at: a single-operand
      *> COLUMN with a LINE clause needs none of the COBOL-2002 multiple-COLUMN / COLUMNS-ARE spellings, so the
      *> only thing that refuses this program below 2023 is the rule under test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB921RPTNE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb921rptne.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-NE.
       WORKING-STORAGE SECTION.
       01  WS-DONE PIC X VALUE "N".
       REPORT SECTION.
       RD  R-NE PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC ZZ9.99 VALUE 10.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-NE.
           GENERATE DET.
           TERMINATE R-NE.
           CLOSE PRT.
           DISPLAY "DONE".
           STOP RUN.
