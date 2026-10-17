*>--- conformance-case
*> id: pb79-group-usage-subordinate-conflict
*> rules: [SR-13.18.29.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.29.3 SR2: under GROUP-USAGE BIT "All subordinate group items shall be explicitly or implicitly
      *> described as GROUP-USAGE BIT" - a subordinate declaring GROUP-USAGE NATIONAL violates it. kb/Work PB79.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 B1 PIC 1(2).
          05 SUB GROUP-USAGE NATIONAL.
             10 N1 PIC N(2).
       PROCEDURE DIVISION.
           STOP RUN.
