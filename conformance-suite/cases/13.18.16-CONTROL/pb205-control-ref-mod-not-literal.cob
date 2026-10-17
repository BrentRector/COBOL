*>--- conformance-case
*> id: pb205-control-ref-mod-not-literal
*> rules: [SR-13.18.16.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR4: "Data-name-1 may be reference-modified. If it is,
      *> leftmost-position and length shall be integer literals." A data-name
      *> leftmost-position is exactly what the second sentence forbids, and the
      *> reason is 13.18.16.4 GR3: the prior control has "the same data
      *> description as the corresponding data item", so the slice's extent has
      *> to be fixed at compile time.
      *> MEASURED BEFORE kb/Work PB205: accepted SILENTLY - the whole ref-mod was
      *> dropped at capture (DataBinder.KeyReference keeps only the qualification
      *> suffix), so the clause bound as CONTROL IS CX and the restriction had
      *> nothing to screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205n1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CX     PIC X(6) VALUE SPACES.
       01 WS-N   PIC 9 VALUE 1.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1 CONTROL IS CX(WS-N:3).
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
