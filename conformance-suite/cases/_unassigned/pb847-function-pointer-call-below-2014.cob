*>--- conformance-case
*> id: pb847-function-pointer-call-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §8.4.3.2 - a function-identifier through function-pointer-name-1 needs a
      *> FUNCTION-POINTER item, a COBOL-2014 usage (§13.18.60.4 GR26). kb/Work PB847 gave the reference its
      *> binder arm; this pins that the arm does not make the construct available below the edition that
      *> introduced the usage - the introduction gate still names it.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBD847N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION PBD847N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB847N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBD847N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PBD847N.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION FP(5)
           STOP RUN.
       END PROGRAM PB847N2.
