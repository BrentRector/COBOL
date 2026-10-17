*>--- conformance-case
*> id: pb852-source-rounded-mode-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.7.4 - the ROUNDED phrase's `MODE IS rounding-mode` half, and its
      *> eight rounding modes, are a COBOL-2014 introduction; a bare ROUNDED at
      *> 85/2002 means the single nearest-away-from-zero rounding.  The report
      *> SOURCE clause's rounded-phrase IS that phrase - 13.18.53.2 says so
      *> verbatim ("where rounded-phrase is described in 14.7.4, ROUNDED
      *> phrase") - so it inherits the gate with no second rule written down:
      *> the grammar references the ONE roundedPhrase production and
      *> VersionConformancePass ParseArm.VisitRoundedPhrase fires on RECOGNITION
      *> wherever it appears (COBOLNET0803).  That is the whole point of sharing
      *> the production rather than writing a report-local copy: a report clause
      *> could not otherwise have acquired the 2014 gate at all.  The same file
      *> compiles at 2014 and 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB852N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb852n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-F PIC 9V99 VALUE 1.55.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC 9(3) SOURCE IS WS-F ROUNDED MODE IS TRUNCATION.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
