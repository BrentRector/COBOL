*>--- conformance-case
*> id: pb205-type-control-operand-other-slice
*> rules: [SR-13.18.57.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.57.3 SR10: "Data-name-1 and data-name-2 may be qualified and
      *> reference-modified. If data-name-1 or data-name-2 is reference-modified,
      *> leftmost-position and length shall be integer literals. Each
      *> data-name-1, data-name-2, and FINAL, if specified, shall be the same as
      *> one of the operands of the CONTROL clause of the corresponding report
      *> description entry." CX(4:3) is not the same operand as CX(1:3).
      *> MEASURED BEFORE kb/Work PB205: accepted - the TYPE operand kept only its
      *> base word and the CONTROL operand had lost its ref-mod, so the footing
      *> matched control level 0 by NAME and printed at the wrong level.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205n5.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX     PIC X(6) VALUE SPACES.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1 CONTROL IS CX(1:3).
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       01 TYPE CF CX(4:3) LINE PLUS 1.
          02 COLUMN 1 PIC 999 SUM WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
