*>--- conformance-case
*> id: pb978-report-control-ambiguous
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB978 - ISO 8.4.2.2.3 SR1 over a report description's CONTROL operand: K OF G names
      *> A.G.K and B.G.K, so the qualification does not preclude ambiguity. The report binder's private
      *> lookup used to take the first and break control on it. Expected: COBOLNET1639 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB978NRC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "pb978nrc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT-FILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 A.
          05 G.
             10 K PIC X VALUE "A".
       01 B.
          05 G.
             10 K PIC X VALUE "B".
       REPORT SECTION.
       RD RPT CONTROL IS K OF G.
       01 TYPE DETAIL LINE 1.
          05 COLUMN 1 PIC X SOURCE K OF A.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT-FILE
           INITIATE RPT
           GENERATE RPT
           TERMINATE RPT
           CLOSE RPT-FILE
           STOP RUN.
