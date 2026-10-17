*>--- conformance-case
*> id: pb853-report-column-no-operand
*> rules: [SR-13.15.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> A PRINTABLE ENTRY WITH A COLUMN CLAUSE AND NO OPERAND (kb/Work PB853), the PICTURE-BEARING shape.
      *> ISO/IEC 1989:2023 §13.15.3 SR10: "Every elementary entry with a COLUMN clause shall also contain
      *> either a SOURCE, VALUE or SUM clause." The rule had no site, and the binder FABRICATED the missing
      *> operand — a figurative SPACE sender — so this entry printed 000 (and once aborted the run unit).
      *> Its PICTURE-less twin is pb853-report-column-no-operand-no-picture; both must draw the SR10
      *> diagnostic, not the SR12 PICTURE one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB853CNO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb853cno.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       REPORT SECTION.
       RD RPT.
       01 DET TYPE DE LINE PLUS 1.
          03 COLUMN 1  PIC 999.
          03 COLUMN 16 PIC X(3) VALUE "END".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
