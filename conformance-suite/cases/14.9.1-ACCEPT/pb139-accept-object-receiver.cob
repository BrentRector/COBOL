*>--- conformance-case
*> id: pb139-accept-object-receiver
*> rules: [SR-14.9.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1.3 SR1: identifier-1 shall not reference a data item of class object (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OREF USAGE OBJECT REFERENCE CBASE.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT OREF
           STOP RUN.
