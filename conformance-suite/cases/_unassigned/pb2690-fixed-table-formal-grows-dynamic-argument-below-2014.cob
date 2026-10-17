*>--- conformance-case
*> id: pb2690-fixed-table-formal-grows-dynamic-argument-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - a fixed table of the formal stored past the capacity of
      *> the argument's dynamic-capacity table creates the occurrence (ISO 14.2.3
      *> GR8 with 8.5.1.9.3), but the OCCURS DYNAMIC CAPACITY clause is a
      *> COBOL-2014 addition (ISO 8.5.1.9), so below 2014 the argument's
      *> description is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GT OCCURS DYNAMIC CAPACITY IN CG FROM 1.
             10 GE PIC X VALUE "a".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB2690SR2" AS NESTED USING BY REFERENCE G
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SR2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 TF OCCURS 3.
             10 FE PIC X.
       PROCEDURE DIVISION USING L.
       MAIN.
           MOVE "r" TO FE (3)
           GOBACK.
       END PROGRAM PB2690SR2.
       END PROGRAM PB2690NEG2.
