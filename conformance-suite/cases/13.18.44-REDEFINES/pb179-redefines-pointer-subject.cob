      *>--- conformance-case
      *> id: pb179-redefines-pointer-subject
      *> rules: [SR-13.18.44.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB179A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(8).
       01 WS-P REDEFINES WS-A USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
