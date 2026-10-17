*>--- conformance-case
*> id: pb2548-template-dynamic-from-to
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2548 - an UNUSED TYPEDEF template. ISO 13.18.38.3 SR28 (cite.py OK): "Integer-4 shall be nonnegative
      *> and integer-5, if both are present, shall be greater than integer-4." FROM 3 TO 2 is not. The template has no
      *> storage (ISO 13.18.58.4 GR2) and no TYPE clause references it, but a syntax rule binds the source as written
      *> (ISO 4.3). Before kb/Work PB2548 the OCCURS DYNAMIC resolver walked the storage forest only, so this compiled
      *> clean. (OCCURS DYNAMIC is COBOL-2014; below it COBOLNET0900 refuses the clause itself.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF.
          05 T PIC X OCCURS DYNAMIC FROM 3 TO 2.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "R01".
           STOP RUN.
