      *>--- conformance-case
      *> id: pb148-display-object
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.11.3 SR1: identifier-1 shall not reference a data item
      *> of class object (kb/Work PB148).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB148N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OREF USAGE OBJECT REFERENCE CBASE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY OREF
           STOP RUN.
