*>--- conformance-case
*> id: pb770-alphabet-noninteger-literal
*> rules: [SR-12.3.7.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR14 b2: "Each noninteger literal shall be an alphanumeric literal." 1.5 is a noninteger
      *> NUMERIC literal, so it is neither the b1 ordinal nor a b2 alphanumeric literal. Accepted silently
      *> before kb/Work PB770 - the alphanumeric arm implemented none of the b-series.
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETNONINTE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALF IS 1.5 ALSO 4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
