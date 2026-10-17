*>--- conformance-case
*> id: pb205-control-operand-subscripted
*> rules: [SR-13.18.16.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> A SUBSCRIPTED control operand is never legal, and it takes two rules to
      *> say so: ISO 13.18.16.3 SR3 - "Data-name-1 shall not be subject to any
      *> OCCURS clauses" - bars the case where the item HAS an OCCURS, and
      *> 8.4.2.3.3 SR2 - a subscripted reference requires the entry to "contain
      *> an OCCURS clause or [be] subordinate to a data description entry that
      *> contains an OCCURS clause" - bars the case where it has none. Between
      *> them no legal subscripted spelling exists.
      *> MEASURED BEFORE kb/Work PB205: the subscript was DROPPED at capture with
      *> the ref-mod (DataBinder.KeyReference keeps only qualification), so the
      *> clause bound as CONTROL IS CT and only the SR3 shape screen fired - on
      *> the item, never on the written subscript.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205n4.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CG.
          05 CR OCCURS 3 TIMES.
             10 CT PIC X(3).
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1 CONTROL IS CT(2).
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
