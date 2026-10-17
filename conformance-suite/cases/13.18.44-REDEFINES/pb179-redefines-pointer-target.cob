      *>--- conformance-case
      *> id: pb179-redefines-pointer-target
      *> rules: [SR-13.18.44.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB179B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P USAGE POINTER.
       01 WS-A REDEFINES WS-P PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
