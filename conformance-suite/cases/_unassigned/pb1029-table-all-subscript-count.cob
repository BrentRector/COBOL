*>--- conformance-case
*> id: pb1029-table-all-subscript-count
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1029 - ISO 8.4.2.3.3 SR3: "the number of subscripts shall equal the number of OCCURS
      *> clauses in the description of the table element being referenced." WS-E has one OCCURS; the
      *> table(ALL) argument writes two subscripts. That arm of the ALL-subscript binder refused
      *> WITHOUT a diagnostic (and so did its undefined-name sibling). Expected: COBOLNET2097, the
      *> resolver's own subscript-count report, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1029NTA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 3 VALUE 1.
       01 WS-N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           COMPUTE WS-N = FUNCTION SUM(WS-E(ALL, ALL))
           DISPLAY WS-N
           STOP RUN.
