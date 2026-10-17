*>--- conformance-case
*> id: pb369-contained-report-file-not-global
*> rules: [SR-14.9.16.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.16.3 SR3: "If data-name-1 is defined in a containing program, the report description entry in
*> which data-name-1 is specified and the file description entry associated with that report description
*> entry shall contain a GLOBAL clause." R-1 is GLOBAL, so the contained program PB369FNB can see DET-1
*> (§13.18.27.4 GR2), but the FD RPT is not, so its GENERATE is refused (kb/Work PB369). INITIATE and
*> TERMINATE restate the rule (§14.9.21.3 SR2, §14.9.46.3 SR2) and share the one check.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369FNA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb369fna.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1 IS GLOBAL PAGE LIMIT IS 10 LINES.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC XX VALUE "DE".
       PROCEDURE DIVISION.
       A-1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           CALL "PB369FNB".
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB369FNB.
       PROCEDURE DIVISION.
       B-1.
           GENERATE DET-1.
           EXIT PROGRAM.
       END PROGRAM PB369FNB.
       END PROGRAM PB369FNA.
