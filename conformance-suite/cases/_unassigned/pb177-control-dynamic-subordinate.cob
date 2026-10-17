*>--- conformance-case
*> id: pb177-control-dynamic-subordinate
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR3: "Data-name-1 shall not be subject to any OCCURS clauses." The operand here is an
      *> item SUBORDINATE to an OCCURS DYNAMIC entry - subject to that entry's OCCURS clause. Its sibling
      *> witness pb177-control-dynamic-table names the table ITSELF; both escaped the first cut of the SR3 arm
      *> (`n.Occurs is not null`, which is NULL for a Format-4 table) and both reached the runtime loud instead
      *> of the compile-time rejection a SYNTAX rule requires. The two are separate witnesses because SR7 can
      *> cover neither and the SR5 arm (occurs-DEPENDING) reaches neither: 8.5.1.12.1's "variable-length group"
      *> is about SUBORDINATES, and OdoModel.TableUnder matches only OccursSpec.DependingName.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N7.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb177n7.rpt".
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
           CONTROL IS CX
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
