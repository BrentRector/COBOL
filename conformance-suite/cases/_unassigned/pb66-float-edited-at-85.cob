*>--- conformance-case
*> id: pb66-float-edited-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.40.4 GR13 b) - the floating-point numeric-edited PICTURE (the symbol E) is a
      *> COBOL-2002 introduction: at --std 85 the version-conformance pass rejects it (COBOLNET0900, registry
      *> row pic-external-float-2002); at 2002+ it compiles (the version-matrix row pins that half). kb/Work PB66.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC -9.9(5)E+99.
       PROCEDURE DIVISION.
           MOVE 1 TO E1.
           STOP RUN.
