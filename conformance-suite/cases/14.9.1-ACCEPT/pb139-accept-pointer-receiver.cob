*>--- conformance-case
*> id: pb139-accept-pointer-receiver
*> rules: [SR-14.9.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1.3 SR1: identifier-1 shall not reference a data item of class pointer (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT P
           STOP RUN.
