*>--- conformance-case
*> id: pb205-sum-reset-operand-other-slice
*> rules: [SR-13.18.54.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR8: "Data-name-3 may be qualified and reference-modified.
      *> Data-name-3 or FINAL shall be an operand of the CONTROL clause of the
      *> current report description. If data-name-3 is reference-modified,
      *> leftmost-position and length shall be integer literals." CX(4:3) is not
      *> an operand of a CONTROL clause whose only operand is CX(1:3).
      *> MEASURED BEFORE kb/Work PB205: accepted - RESET ON kept only the base
      *> word, so it matched the control level by NAME and reset the counter at a
      *> level the program never named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205n6.rpt".
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
       01 TYPE CF CX(1:3) LINE PLUS 1.
          02 COLUMN 1 PIC 999 SUM WS-SRC RESET ON CX(4:3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
