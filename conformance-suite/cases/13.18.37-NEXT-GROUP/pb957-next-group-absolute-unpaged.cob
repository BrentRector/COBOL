*>--- conformance-case
*> id: pb957-next-group-absolute-unpaged
*> rules: [SR-13.18.37.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> AN ABSOLUTE NEXT GROUP IN A REPORT THAT IS NOT DIVIDED INTO PAGES (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR3: "If the report is not divided into pages, only the relative
      *> form of the clause may be specified." The RD has no PAGE clause.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NAU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nau.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT.
       01  DET TYPE DE NEXT GROUP 5.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
