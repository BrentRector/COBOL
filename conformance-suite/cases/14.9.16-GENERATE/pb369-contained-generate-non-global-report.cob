*>--- conformance-case
*> id: pb369-contained-generate-non-global-report
*> rules: [SR-14.9.16.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO §13.18.27.4 GR2: only "a program contained directly or indirectly within a program that describes a
*> global name may reference that name without describing it again". R-1 has NO GLOBAL clause, so its
*> detail group DET-1 is not a name the contained program PB369NGB can reference, and its GENERATE names
*> neither a detail report group nor a report-name it can see (ISO §14.9.16.3 SR1/SR2). kb/Work PB369;
*> the legal shape - RD ... IS GLOBAL - is conformance:85/pb369_global_report_use_selection.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369NGA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb369nga.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 10 LINES.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC XX VALUE "DE".
       PROCEDURE DIVISION.
       A-1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           CALL "PB369NGB".
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369NGB.
       PROCEDURE DIVISION.
       B-1.
           GENERATE DET-1.
           EXIT PROGRAM.
       END PROGRAM PB369NGB.
       END PROGRAM PB369NGA.
