*> reject-at: 85
      *> kb/Work PB1653 - the NEGATIVE twin of tests/conformance/2002/pb1653_national_group_view.
      *> A national group (GROUP-USAGE NATIONAL, ISO 13.18.29) and USAGE NATIONAL items are COBOL-2002
      *> introductions that do not exist in COBOL-85, so the view of one over alphanumeric storage is rejected at
      *> --std 85, gated by COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1653NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A           PIC X(10).
       01  NGV REDEFINES A GROUP-USAGE NATIONAL.
           05  V1      PIC N(2).
           05  V2      PIC N(3).
       PROCEDURE DIVISION.
           STOP RUN.
