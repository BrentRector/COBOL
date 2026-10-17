      *>--- conformance-case
      *> id: pb2937-skeleton-only-receiver
      *> rules: [GR-11.3.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2937 - a SET between two references described with
      *> the same parameter-name is asked of the UNEXPANDED definition:
      *> both name one class, whatever the actual is (9.3.8.2.4, cite.py
      *> OK), so what does not turn on which class it is has one answer
      *> in every expansion. ONE is described "T ONLY" and X is "T", and
      *> 14.9.39.3 SR12 a)1. (cite.py OK) requires an ONLY receiver's
      *> sender to be described with the ONLY phrase: COBOLNET0867.
       IDENTIFICATION DIVISION.
       CLASS-ID. NSKONLY USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ONE USAGE OBJECT REFERENCE T ONLY.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE T.
       PROCEDURE DIVISION USING X.
           SET ONE TO X.
       END METHOD PUT.
       END OBJECT.
       END CLASS NSKONLY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSKONLYM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM NSKONLYM.
