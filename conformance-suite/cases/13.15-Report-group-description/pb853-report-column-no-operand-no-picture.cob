*>--- conformance-case
*> id: pb853-report-column-no-operand-no-picture
*> rules: [SR-13.15.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> A PRINTABLE ENTRY WITH A COLUMN CLAUSE AND NO OPERAND (kb/Work PB853), the PICTURE-LESS shape.
      *> ISO/IEC 1989:2023 §13.15.3 SR10: "Every elementary entry with a COLUMN clause shall also contain
      *> either a SOURCE, VALUE or SUM clause." This shape used to be refused only by accident, under the
      *> SR12 PICTURE rule's staging code (COBOLNET0899); SR10 is the rule that decides it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB853CNP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb853cnp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       REPORT SECTION.
       RD RPT.
       01 DET TYPE DE LINE PLUS 1.
          03 COLUMN 1.
          03 COLUMN 6 PIC X(3) VALUE "END".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
