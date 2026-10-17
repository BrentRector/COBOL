      *>--- conformance-case
      *> id: pb148-display-pointer
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.11.3 SR1: identifier-1 shall not reference a data item
      *> of class pointer - it previously printed the CLR carrier's
      *> ToString (kb/Work PB148).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB148N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY P
           STOP RUN.
