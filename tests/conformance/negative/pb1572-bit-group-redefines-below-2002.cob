*> reject-at: 85
      *> kb/Work PB1572 - the NEGATIVE twin of tests/conformance/2002/pb1572_bit_group_redefines_offsets.
      *> The bit placement ISO 8.5.1.6.3 gives a bit GROUP inside a REDEFINES'd record is reachable only through
      *> GROUP-USAGE BIT (13.18.29) and USAGE BIT (13.18.60) - COBOL-2002 introductions that do not exist in
      *> COBOL-85 - each gated by COBOLNET0900 at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1572NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R3.
           05  F3          PIC 1 USAGE BIT.
           05  S3 GROUP-USAGE BIT.
               10  P1      PIC 1 USAGE BIT.
               10  P2      PIC 1 USAGE BIT.
       01  R3V REDEFINES R3 PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
           STOP RUN.
