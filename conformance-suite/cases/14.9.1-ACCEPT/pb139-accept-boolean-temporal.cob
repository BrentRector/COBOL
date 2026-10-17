*>--- conformance-case
*> id: pb139-accept-boolean-temporal
*> rules: [SR-14.9.1.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1.3 SR3: identifier-2 (the temporal receiver) shall not reference a data item of class
      *> boolean (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT B FROM TIME
           STOP RUN.
