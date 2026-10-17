*>--- conformance-case
*> id: pb1002-unpaged-absolute-line
*> rules: [SR-13.18.35.3-5]
*> source-format: free
*> portable: unverified
*>---
*> AN ABSOLUTE LINE IN A REPORT THAT IS NOT DIVIDED INTO PAGES (kb/Work PB1002). ISO/IEC 1989:2023
*> §13.18.35.3 SR5: "If the report is not divided into pages, all its LINE clauses shall be relative." The RD
*> has no PAGE clause, so the report is one page of indefinite length (§13.18.39.4 GR2a) and LINE 3 names a
*> line of no page. It compiled clean before PB1002. A rule of every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1002UA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1002-unpaged-absolute-line.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1.
       01  DE-1 TYPE DETAIL LINE 3.
           05  COLUMN 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DE-1.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
