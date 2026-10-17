*>--- conformance-case
*> id: pb565-report-line-in-line
*> rules: [SR-13.15.3-8, SR-13.18.35.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ A LINE CLAUSE MAY NOT NEST INSIDE A LINE CLAUSE (kb/Work PB565). ISO/IEC 1989:2023 §13.18.35.3 SR4:
*> "Within a given report group description entry, an entry that contains a LINE clause shall not have a
*> subordinate entry that also contains a LINE clause."
*>
*> The rule is what makes §13.18.38.4 GR10's vertical pair exhaustive rather than overlapping: GR10c is "the
*> entry also contains a relative LINE clause" and GR10d is "the entry is a group entry having subordinate
*> entries with relative LINE clauses", and SR4 guarantees an entry is at most one of the two. Without it a
*> repeating entry could carry a LINE clause AND have subordinate ones, and §13.18.38.4 GR12's "integer-3 lines
*> vertically beneath the preceding occurrence" would have two different lines to measure from.
*>
*> It is a rule of EVERY edition — the single-operand LINE clause is COBOL-85 syntax — so this program is
*> rejected at all four, on COBOLNET2199, the LINE clause syntax-rule family. Deleting the inner LINE clause
*> makes it conforming: one report line at LINE-COUNTER + 1 carrying N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565LIL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb565lil.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-LIL.
       REPORT SECTION.
       RD  R-LIL PAGE LIMIT IS 20 LINES.
       01  D-LIL TYPE DE.
           03  LINE PLUS 1.
               05  LINE PLUS 1.
                   07  COLUMN 1 PIC X(1) VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-LIL.
           GENERATE D-LIL.
           TERMINATE R-LIL.
           CLOSE RPT.
           STOP RUN.
