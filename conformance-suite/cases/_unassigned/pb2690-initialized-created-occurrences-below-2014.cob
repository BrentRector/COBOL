*>--- conformance-case
*> id: pb2690-initialized-created-occurrences-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2690 - the occurrences a BY REFERENCE formal's fixed table
      *> creates in an INITIALIZED dynamic-capacity table of the argument hold
      *> the INITIALIZED default (ISO 14.2.3 GR8 with 8.5.1.9.3 and 8.5.1.9.5),
      *> but the OCCURS DYNAMIC CAPACITY clause and its INITIALIZED phrase are
      *> COBOL-2014 additions (ISO 8.5.1.9), so below 2014 the argument's
      *> description is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690NEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GX PIC X VALUE "x".
          05 GT OCCURS DYNAMIC CAPACITY IN CG FROM 1 INITIALIZED.
             10 GE1 PIC X VALUE "a".
             10 GF1 PIC X VALUE "b".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB2690SR3" AS NESTED USING BY REFERENCE G
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SR3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LX PIC X.
          05 TF OCCURS 3.
             10 FE PIC X.
             10 FF PIC X.
       PROCEDURE DIVISION USING L.
       MAIN.
           MOVE "r" TO FE (3)
           GOBACK.
       END PROGRAM PB2690SR3.
       END PROGRAM PB2690NEG3.
