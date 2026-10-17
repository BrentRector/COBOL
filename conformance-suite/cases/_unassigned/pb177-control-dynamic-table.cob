*>--- conformance-case
*> id: pb177-control-dynamic-table
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR3: "Data-name-1 shall not be subject to any OCCURS clauses." The operand here IS the
      *> OCCURS DYNAMIC entry - 13.18.38.2 Format 4 (dynamic-capacity-table) is a form of the OCCURS clause, so
      *> the entry is subject to one. kb/Work PB177 arm C's first cut spelled the SR3 arm `n.Occurs is not null`,
      *> and DataItem.Occurs is the FIXED physical capacity - NULL for a Format-4 table - so this shape escaped
      *> the compile-time screen entirely. Measured: it compiled clean and reached ReportWriterEmitter's runtime
      *> loud, where a SYNTAX rule requires the compile-time rejection.
      *> ⛔ SR7 STRUCTURALLY CANNOT COVER THIS ONE, which is why it needs its own witness: 8.5.1.12.1 defines a
      *> "variable-length group" over items SUBORDINATE to the group, so the dynamic table ENTRY ITSELF is never
      *> one - the same subject/subordinate asymmetry 13.18.44.3 SR17 documents. The arm now reads DataItem.IsTable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb177n6.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CG.
          05 CR OCCURS DYNAMIC CAPACITY IN CCAP FROM 1 TO 5.
             10 CX PIC X(3).
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           CONTROL IS CR
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           GOBACK.
