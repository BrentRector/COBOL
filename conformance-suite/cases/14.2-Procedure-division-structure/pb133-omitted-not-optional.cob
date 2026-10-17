*>--- conformance-case
*> id: pb133-omitted-not-optional
*> rules: [GR-14.2.3-3, SR-14.9.4.3-24, GR-14.9.4.4-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR24: if the OMITTED phrase is specified, the OPTIONAL phrase shall be specified for
      *> the corresponding formal parameter - checkable at BIND with the AS NESTED callee's header known
      *> (kb/Work PB133 wave C; this closes the row PB130 left PARTIAL).
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
       01 L-B PIC 9(4).
       01 L-C PIC 9(4).
       PROCEDURE DIVISION USING L-A L-B OPTIONAL L-C.
       P.
           IF L-B IS OMITTED DISPLAY "B-OMITTED" END-IF
           IF L-C IS OMITTED DISPLAY "C-OMITTED" END-IF
           IF L-A IS NOT OMITTED DISPLAY "A=" L-A END-IF
           GOBACK.
       END PROGRAM S1.
       END PROGRAM OM1.
