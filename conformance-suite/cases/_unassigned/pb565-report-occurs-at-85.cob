*>--- conformance-case
*> id: pb565-report-occurs-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE REPORT GROUP DESCRIPTION ENTRY GAINED ITS OCCURS CLAUSE AT COBOL-2002 (kb/Work PB565).
*> ISO/IEC 1989:2023 §13.18.38.2 prints the report-writer OCCURS as format 3 — "OCCURS [ integer-1 TO ]
*> integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]" — and §13.15.2's report group description
*> entry general format carries it. The COBOL-85 report group description entry had no OCCURS clause at all,
*> which is why its whole repeating-entry family enters at 2002 together: the multiple LINE clause and the
*> multiple/relative COLUMN forms (§13.18.35.3 / §13.18.14.3 SR10), the VARYING clause (§13.18.64), the
*> PRESENT WHEN clause (§13.18.41) — each already a `*-2002` construct row here — and this.
*>
*> So a COBOL-85 compilation shall name the edition rather than silently repeat the item: construct
*> `report-occurs-2002`, diagnostic COBOLNET0900. Above 85 the same source compiles and prints OK OK OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565O85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb565o85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-85.
       REPORT SECTION.
       RD  R-85 PAGE LIMIT IS 20 LINES.
       01  D-85 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(2) OCCURS 3 TIMES STEP 3 VALUE "OK".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-85.
           GENERATE D-85.
           TERMINATE R-85.
           CLOSE RPT.
           STOP RUN.
