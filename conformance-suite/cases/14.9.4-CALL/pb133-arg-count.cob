*>--- conformance-case
*> id: pb133-arg-count
*> rules: [SR-14.9.4.3-25]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.8.2.1: the argument count shall equal the formal-parameter count, except trailing OPTIONAL
      *> formals omitted - four arguments against three formals is a bind-time diagnostic under AS NESTED
      *> (the dynamic Format-1 count check is EC-PROGRAM-ARG-MISMATCH at activation, wave C2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING W-A OMITTED W-A W-A
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-B PIC 9(4).
       01 L-C PIC 9(4).
       PROCEDURE DIVISION USING L-A OPTIONAL L-B OPTIONAL L-C.
       P.
           IF L-B IS OMITTED DISPLAY "B-OMITTED" END-IF
           IF L-C IS OMITTED DISPLAY "C-OMITTED" END-IF
           IF L-A IS NOT OMITTED DISPLAY "A=" L-A END-IF
           GOBACK.
       END PROGRAM S1.
       END PROGRAM OM1.
