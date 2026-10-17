*>--- conformance-case
*> id: pb565-multiple-line-with-occurs
*> rules: [SR-13.18.35.3-10, SR-13.18.38.3-26]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ A MULTIPLE LINE CLAUSE AND AN OCCURS CLAUSE MAY NOT SHARE AN ENTRY (kb/Work PB565). ISO/IEC 1989:2023
*> §13.18.35.3 SR10 d): "An OCCURS clause shall not also be present in the same entry." The rule is what keeps
*> §13.15.4 GR3's repetition count single-valued: §13.18.35.4 GR9 already reads the multiple LINE clause AS "a
*> simple OCCURS clause whose integer is equal to the number of operands of the LINE clause", so an entry
*> carrying both would name two different repetition counts for one entry and §13.18.63.3 SR35's operand-count
*> screen would have no answer.
*>
*> Diagnostic COBOLNET2199, the LINE clause syntax-rule family. Dropping either clause makes the source
*> conforming: `LINE 2 4 7.` prints three lines, `LINE 2 OCCURS 3 TIMES STEP 8.` prints lines 2, 10 and 18.
*> (STEP 8 rather than STEP 2 so that §13.18.38.3 SR26 — integer-3 shall prevent two consecutive repetitions
*> overlapping, and the three LINE operands span six lines — does not also fire and mask the rule under test.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565MLO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb565mlo.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-MLO.
       REPORT SECTION.
       RD  R-MLO PAGE LIMIT IS 20 LINES.
       01  D-MLO TYPE DE.
           03  LINE 2 4 7 OCCURS 3 TIMES STEP 8.
               05  COLUMN 1 PIC X(1) VALUE "M".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-MLO.
           GENERATE D-MLO.
           TERMINATE R-MLO.
           CLOSE RPT.
           STOP RUN.
