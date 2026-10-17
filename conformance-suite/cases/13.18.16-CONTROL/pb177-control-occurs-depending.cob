*>--- conformance-case
*> id: pb177-control-occurs-depending
*> rules: [SR-13.18.16.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR5: "The entry specified by data-name-1 shall not have an occurs-depending table
      *> subordinate to it." Unscreened until kb/Work PB177 arm C - measured: it compiled and ran silently.
      *> NOTE THE PREMISE CHECK (feedback_validate_the_premise_not_only_the_rule): CG is NOT a variable-length
      *> group, so SR7 does not reach it - 8.5.1.12.1 defines that term over dynamic-length elementary items
      *> and dynamic-CAPACITY tables, and an occurs-DEPENDING table is neither. SR5 exists precisely because
      *> SR7 does not cover this shape; a screen written for only one of them leaves the other open.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb177n4.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 NN PIC 9 VALUE 2.
       01 CG.
          05 CT OCCURS 1 TO 5 DEPENDING ON NN PIC X(3).
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
           STOP RUN.
