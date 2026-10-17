*>--- conformance-case
*> id: pb139-accept-strong-group
*> rules: [SR-14.9.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1.3 SR1: identifier-1 shall not reference a strongly-typed group item (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          02 F1 PIC 9(4).
       01 V TYPE TPT.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT V
           STOP RUN.
