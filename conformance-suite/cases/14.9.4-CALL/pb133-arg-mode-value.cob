*>--- conformance-case
*> id: pb133-arg-mode-value
*> rules: [SR-14.9.4.3-21]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR21: with BY VALUE specified for an argument, BY VALUE shall be specified for the
      *> corresponding formal parameter - a bind-time diagnostic under AS NESTED (kb/Work PB133 wave C2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING BY VALUE W-A OMITTED
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
