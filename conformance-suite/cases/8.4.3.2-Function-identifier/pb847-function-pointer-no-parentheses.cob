*>--- conformance-case
*> id: pb847-function-pointer-no-parentheses
*> rules: [SR-8.4.3.2.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §8.4.3.2.3 SR5 - "If function-pointer-name-1 is specified, the parentheses shall be
      *> specified." A prototype or intrinsic function-identifier may omit an empty argument list; one naming a
      *> function-pointer may not, because the bare name is the reference to the POINTER itself. kb/Work PB847.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBZ847N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 77 TO L-RES
           GOBACK.
       END FUNCTION PBZ847N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB847N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBZ847N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FZ USAGE FUNCTION-POINTER TO PBZ847N.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
       MAIN.
           SET FZ TO ADDRESS OF FUNCTION PBZ847N
           COMPUTE R = FUNCTION FZ
           STOP RUN.
       END PROGRAM PB847N1.
