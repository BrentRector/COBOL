*>--- conformance-case
*> id: pb133_omitted_lenient
*> rules: [GR-14.9.4.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave C - ISO 14.9.4.4 GR12 with checking NOT enabled (14.6.13.1.1: "if checking
      *> for an exception that occurs is not enabled, no exception condition is raised"): the content of an
      *> omitted formal is undefined, and COBOL.NET's DOCUMENTED implementor choice (the CA10 lenient
      *> posture) is the type's benign empty value - a numeric view answers zero. With
      *> >>TURN EC-PROGRAM-ARG-OMITTED CHECKING ON the same reference is the fatal EC in the CALLEE's own
      *> engine (never the caller's ON EXCEPTION phrase - GR3i).
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
       PROCEDURE DIVISION USING L-A OPTIONAL L-B OPTIONAL L-C.
       P.
           DISPLAY "B-LENIENT=" L-B
           IF L-C IS OMITTED DISPLAY "C-OMITTED" END-IF
           IF L-A IS NOT OMITTED DISPLAY "A=" L-A END-IF
           GOBACK.
       END PROGRAM S1.
       END PROGRAM OM1.
