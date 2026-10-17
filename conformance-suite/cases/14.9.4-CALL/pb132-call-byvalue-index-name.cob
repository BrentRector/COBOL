*>--- conformance-case
*> id: pb132-call-byvalue-index-name
*> rules: [SR-14.9.4.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR22: a BY VALUE operand shall be of class numeric, object, or pointer. An
      *> index-name is class index (8.5.2.1 Table 2's own row) - it bound to BoundIndexRef and slipped
      *> the screen's BoundNumRef guard (kb/Work PB132).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING BY VALUE IX
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC 9(9) USAGE BINARY.
       PROCEDURE DIVISION USING BY VALUE LV.
       P.
           GOBACK.
       END PROGRAM S1.
       END PROGRAM PB132N8.
