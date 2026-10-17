*>--- conformance-case
*> id: pb485-level-66-report-group
*> rules: [SR-13.15.3-3, SR-13.18.33.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.3 SR4: "Report group description entries that are
      *> subordinate to an RD entry shall have level-numbers with the
      *> values 1 through 49." The report arm admits NO special level at
      *> all -- not 66, not 77, not 88 -- so a 66 entry that SR5 would
      *> welcome in working-storage is illegal here. reportGroupEntry is
      *> a distinct grammar rule from dataDescriptionEntry and reaches a
      *> distinct binder, which is why the screen sits on the levelNumber
      *> node all four arms share. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb485n4.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-BAD.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC 99 VALUE 7.
       REPORT SECTION.
       RD  R-BAD PAGE LIMIT IS 20 LINES.
       01  DET-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE IS WS-AMT.
       66  R-BAD-LEVEL.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT
           INITIATE R-BAD
           GENERATE DET-A
           TERMINATE R-BAD
           CLOSE RPT
           STOP RUN.
