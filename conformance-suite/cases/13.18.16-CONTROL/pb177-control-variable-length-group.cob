*>--- conformance-case
*> id: pb177-control-variable-length-group
*> rules: [SR-13.18.16.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR7: "Data-name-1 shall not reference a variable-length group." Until kb/Work PB177
      *> arm C this compiled and then staged a RUNTIME Tier-C loud - but SR7 is a SYNTAX rule, so the verdict
      *> is a compile-time rejection; an emitter loud is a backstop, never the verdict.
      *> CG here IS a variable-length group per 8.5.1.12.1 (a DYNAMIC LENGTH elementary item is subordinate).
      *> Its sibling fixture pb177-control-occurs-depending carries the SR5 shape, which SR7 does NOT cover.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb177n5.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 CG.
          05 CT PIC X DYNAMIC LENGTH.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           CONTROL IS CG
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
