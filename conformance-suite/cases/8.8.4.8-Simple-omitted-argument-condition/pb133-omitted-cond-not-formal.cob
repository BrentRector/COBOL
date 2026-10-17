*>--- conformance-case
*> id: pb133-omitted-cond-not-formal
*> rules: [SR-8.8.4.8.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.8 SR1: data-name-1 shall be a FORMAL PARAMETER defined in the source element in which
      *> the condition is specified - a linkage item outside the procedure division header's USING is not
      *> one (kb/Work PB133 wave C).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING W-A OMITTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 W-X PIC 9(4).
       01 L-B PIC 9(4).
       01 L-C PIC 9(4).
       PROCEDURE DIVISION USING L-A OPTIONAL L-B OPTIONAL L-C.
       P.
           IF W-X IS OMITTED DISPLAY "B-OMITTED" END-IF
           IF L-C IS OMITTED DISPLAY "C-OMITTED" END-IF
           IF L-A IS NOT OMITTED DISPLAY "A=" L-A END-IF
           GOBACK.
       END PROGRAM S1.
       END PROGRAM OM1.
