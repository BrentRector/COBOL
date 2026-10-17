*>--- conformance-case
*> id: present-when-at-85
*> rules: [FMT-13.18.41.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.41 Format 1 — the report-group PRESENT WHEN clause is a
      *> COBOL-2002 introduction (the 2002 RW modernization; PRESENT itself is
      *> §8.9-reserved "added 2002"). Below 2002 the recognition gate rejects
      *> (COBOLNET0900, VersionConformancePass ParseArm).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWPW85P10RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rwpw85-p10rp.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 W-F PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1.
       01 D-1 TYPE DE.
          03 LINE PLUS 1.
             05 COLUMN 1 PIC X(2) VALUE "OK"
                PRESENT WHEN W-F = 1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE D-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
