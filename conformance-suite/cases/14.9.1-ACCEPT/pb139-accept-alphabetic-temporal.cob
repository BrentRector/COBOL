*>--- conformance-case
*> id: pb139-accept-alphabetic-temporal
*> rules: [SR-14.9.1.3-3, GR-14.9.1.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1.3 SR3: identifier-2 (the temporal receiver) shall not reference a data item of class
      *> alphabetic. The device format (SR1) permits PIC A - only Format 2 screens it (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC A(6).
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT A FROM DATE
           STOP RUN.
