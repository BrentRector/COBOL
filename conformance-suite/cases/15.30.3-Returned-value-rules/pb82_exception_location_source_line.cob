      *>--- conformance-case
      *> id: pb82_exception_location_source_line
      *> rules: [RV-15.30.3-2]
      *> accept: [2023]
      *> files: [pb82loc_data.cpy, pb82loc_proc.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * PB82 - FUNCTION EXCEPTION-LOCATION's third part (15.30.3 r2b3,
      * "an implementor-defined identifier of the source line that
      * contains the beginning of the statement") is the line of the
      * statement's first token in the file that PHYSICALLY holds it
      * (docs/CONFORMANCE.md determination), not the ordinal of the
      * resultant text after COPY / REPLACE / continuation processing: a
      * bare number for the main source file, "copybook(line)" for a
      * statement inside COPY-incorporated text. This program shifts the
      * resultant text three ways before each RAISE - a 3-line data COPY
      * (line 25), a REPLACE statement (line 32, which vanishes from the
      * resultant text) and a fixed-form continuation (lines 37-38,
      * which JOIN) - and every reported line is still a line of THIS
      * file: 35, 40, and the procedure copybook's own line 2 for the
      * copied RAISE. Fixed-form: every line stays within column 72.
       >>TURN EC-USER CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB82LOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(20).
       COPY "pb82loc_data.cpy".
       01 WS-E PIC X(30) VALUE SPACES.
       PROCEDURE DIVISION.
      * The data COPY above incorporated three lines; without the origin
      * map the resultant ordinal of the RAISE below would be 37.
           MOVE CPY-A TO WS-D.
           DISPLAY "D=[" WS-D "]".
       REPLACE ==XYZZY== BY ==PLUGH==.
      * The REPLACE statement's own line vanishes from the resultant
      * text; the RAISE below is on source line 35.
           RAISE EXCEPTION EC-USER-L.
           DISPLAY "A=[" FUNCTION EXCEPTION-LOCATION "]".
           MOVE "a continued literal spanning two physi
      -    "cal lines" TO WS-E.
           DISPLAY "E=[" WS-E "]".
           RAISE EXCEPTION EC-USER-L.
           DISPLAY "B=[" FUNCTION EXCEPTION-LOCATION "]".
       COPY "pb82loc_proc.cpy".
           STOP RUN.
