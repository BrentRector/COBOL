      *>--- conformance-case
      *> id: turn-location-unspecified
      *> rules: [GR-7.3.25.4-7, RV-15.30.3-1, DOC-A.1-204]
      *> accept: [2002, 2014, 2023]
      *> implementor-defined: [204]
      *> constructs: [turn-directive-2002]
      *> source-format: fixed
      *>---
      *> ISO 7.3.25.4 GR 7: "If the LOCATION phrase is not specified,
      *> the implementor shall specify whether this information is made
      *> available or not" (Annex A.1 item 204), two values, one
      *> expected output each. 15.30.3 rule 1: without saved location
      *> information EXCEPTION-LOCATION returns one alphanumeric space.
      *> The STRING overflows its receiver, so with EC-OVERFLOW-STRING
      *> checking on that condition is the last exception status.
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSTLOCU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-DST PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "ABCDEFG" DELIMITED BY SIZE INTO W-DST
               ON OVERFLOW CONTINUE
           END-STRING
           IF FUNCTION EXCEPTION-STATUS = "EC-OVERFLOW-STRING"
               DISPLAY "RAISED"
           ELSE
               DISPLAY "NOT RAISED"
           END-IF
           IF FUNCTION EXCEPTION-LOCATION = SPACE
               DISPLAY "LOCATION NOT AVAILABLE"
           ELSE
               DISPLAY "LOCATION AVAILABLE"
           END-IF
           STOP RUN.
