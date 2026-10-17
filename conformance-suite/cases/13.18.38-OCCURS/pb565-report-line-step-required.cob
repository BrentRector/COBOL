*>--- conformance-case
*> id: pb565-report-line-step-required
*> rules: [SR-13.18.38.3-25]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ AN ABSOLUTE LINE CLAUSE ON A REPEATING ENTRY REQUIRES THE STEP PHRASE (kb/Work PB565). ISO/IEC 1989:2023
*> §13.18.38.3 SR25: "The STEP phrase shall be specified if the entry: a) contains an absolute LINE clause, or
*> b) has an entry with an absolute LINE clause subordinate to it, or c) contains an absolute COLUMN clause, or
*> d) is subordinate to an entry with a LINE clause and has an entry with an absolute COLUMN clause subordinate
*> to it." Without integer-3 §13.18.38.4 GR12 gives the repetitions no vertical interval at all — GR12's closing
*> sentence takes it "from the relative LINE or COLUMN numbers … specified in the corresponding report section
*> entries", and an ABSOLUTE line number supplies none — so all three occurrences would print on line 5.
*>
*> The leg under test is SR25a, the entry's OWN absolute LINE clause. Its sibling SR25b (an absolute LINE
*> clause SUBORDINATE to the repeating entry) reports the same way. Diagnostic COBOLNET2021, the report-writer
*> OCCURS syntax-rule family. Adding STEP 2 makes the same source conforming: lines 5, 7, 9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565LSR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb565lsr.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-LSR.
       REPORT SECTION.
       RD  R-LSR PAGE LIMIT IS 20 LINES.
       01  D-LSR TYPE DE.
           03  LINE 5 OCCURS 3 TIMES.
               05  COLUMN 1 PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-LSR.
           GENERATE D-LSR.
           TERMINATE R-LSR.
           CLOSE RPT.
           STOP RUN.
