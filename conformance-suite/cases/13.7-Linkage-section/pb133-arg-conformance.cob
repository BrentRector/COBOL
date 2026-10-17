*>--- conformance-case
*> id: pb133-arg-conformance
*> rules: [SR-13.7.3-3, GR-14.8.2.3.2-L2.2, SR-14.9.4.3-25]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.8.2.3.2 (elementary items passed BY REFERENCE, the NESTED-call regime): the definitions
      *> shall have the same PICTURE - a 9(6) argument against a 9(4) formal is EC-PROGRAM-ARG-MISMATCH
      *> at run time and a diagnostic at bind under AS NESTED, through THE one relation: the bind-time
      *> entry ParameterConformance asks the run-time ActivationRelations.CallArgumentViolation
      *> (kb/Work PB133 wave C2, PB3101).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(6) VALUE 5.
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
       PROCEDURE DIVISION USING L-A OPTIONAL L-B OPTIONAL L-C.
       P.
           IF L-B IS OMITTED DISPLAY "B-OMITTED" END-IF
           IF L-C IS OMITTED DISPLAY "C-OMITTED" END-IF
           IF L-A IS NOT OMITTED DISPLAY "A=" L-A END-IF
           GOBACK.
       END PROGRAM S1.
       END PROGRAM OM1.
