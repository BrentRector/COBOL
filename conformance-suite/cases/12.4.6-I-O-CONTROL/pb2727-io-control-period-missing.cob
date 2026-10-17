*>--- conformance-case
*> id: pb2727-io-control-period-missing
*> rules: [FMT-12.4.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - ISO 12.4.6.2 closes the I-O-CONTROL paragraph's run of SAME clauses with a
      *> separator period (5.2.1).  A paragraph with no period at all before the DATA DIVISION header
      *> compiled clean at every edition; it is refused by name at its last clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727IM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N2727IM1.dat".
           SELECT F2 ASSIGN TO "N2727IM2.dat".
       I-O-CONTROL.
           SAME AREA FOR F1 F2
       DATA DIVISION.
       FILE SECTION.
       FD F1. 01 R1 PIC X(4).
       FD F2. 01 R2 PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
