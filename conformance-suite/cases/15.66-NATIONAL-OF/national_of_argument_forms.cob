*>--- conformance-case
*> id: national_of_argument_forms
*> rules: [FMT-15.66.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.66 NATIONAL-OF — both shapes of the general format (§15.66.2):
      *>     FUNCTION NATIONAL-OF ( argument-1 [ argument-2 ] )
      *> §15.66.3 r2: argument-2 is a one-character national substitution character; it substitutes only
      *> for characters with no national correspondent, and every character of "XY" has one, so both
      *> forms return N"XY" — the presence of argument-2 is exactly what this golden pins.
      *> §15.66.4 r1: each alphanumeric character converts to its corresponding national representation;
      *> §15.66.4 r4: the length is the national character positions required — 2, proven by LENGTH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NATOFFORMS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N2  PIC N(2).
       01 L   PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION NATIONAL-OF("XY") TO N2
           DISPLAY "F1=" N2
           MOVE FUNCTION NATIONAL-OF("XY" N"#") TO N2
           DISPLAY "F2=" N2
           MOVE FUNCTION LENGTH(FUNCTION NATIONAL-OF("XY" N"#")) TO L
           DISPLAY "LN=" L
           STOP RUN.
