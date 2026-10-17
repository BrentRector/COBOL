*>--- conformance-case
*> id: pb2727-io-control-period-per-clause
*> rules: [FMT-12.4.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - ISO 12.4.6.2 prints the I-O-CONTROL paragraph's run of SAME clauses as ONE
      *> sentence closed by one separator period (only the APPLY COMMIT clause is a sentence of its
      *> own), and 5.2.1 binds the punctuation a general format prints.  A period after each SAME
      *> clause compiled clean at every edition; it is refused by name at the inner period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727IP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "N2727IP1.dat".
           SELECT F2 ASSIGN TO "N2727IP2.dat".
           SELECT F3 ASSIGN TO "N2727IP3.dat".
           SELECT F4 ASSIGN TO "N2727IP4.dat".
       I-O-CONTROL.
           SAME AREA FOR F1 F2.
           SAME RECORD AREA FOR F3 F4.
       DATA DIVISION.
       FILE SECTION.
       FD F1. 01 R1 PIC X(4).
       FD F2. 01 R2 PIC X(4).
       FD F3. 01 R3 PIC X(4).
       FD F4. 01 R4 PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
