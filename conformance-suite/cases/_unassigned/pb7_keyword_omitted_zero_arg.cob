*>--- conformance-case
*> id: pb7_keyword_omitted_zero_arg
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB7 - a ZERO-ARGUMENT intrinsic in the keyword-omitted form (ISO 12.3.8.1 + 8.4.3.2.3 SR2).
      *> 15.21.2's general format is "FUNCTION CURRENT-DATE" with NO parentheses, so with the keyword
      *> omitted the reference is a BARE NAME - zero suffixes, not one. KeywordOmittedFunction required
      *> exactly one suffix, so every zero-argument intrinsic fell through to a data reference, resolved to
      *> nothing, COMPILED CLEAN and threw NotImplementedCobolFeatureException at RUN TIME. The standard
      *> writes this form itself at D.14.3.6: MOVE FUNCTION LOCALE-DATE (CURRENT-DATE (1:8)).
      *>
      *> Expected values are structural, because the VALUES are a clock and an irrational:
      *>   15.21.3 fixes CURRENT-DATE's length at 21 character positions.
      *>   15.73.3 makes PI an implementor-defined approximation, so the golden pins the leading digits only,
      *>   via a comparison rather than by printing our own double back at ourselves. The bounds are INCLUSIVE
      *>   and the receiver carries 6 decimals on purpose: at 4 decimals pi STORES as exactly 3.1416, so an
      *>   exclusive upper bound of 3.1416 excluded the correct value. A golden must derive how a value is
      *>   STORED, not only what it is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB7KWOMIT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CD PIC X(21).
       01 R     PIC S9V9(6).
       01 LEN   PIC 9(3).
       PROCEDURE DIVISION.
           MOVE CURRENT-DATE TO WS-CD
           COMPUTE LEN = FUNCTION LENGTH(WS-CD)
           DISPLAY LEN
           COMPUTE R = PI
           IF R >= 3.141592 AND R <= 3.141593 DISPLAY "PI-IN-RANGE"
              ELSE DISPLAY "PI-WRONG" END-IF
           COMPUTE R = E
           IF R >= 2.718281 AND R <= 2.718282 DISPLAY "E-IN-RANGE"
              ELSE DISPLAY "E-WRONG" END-IF
           STOP RUN.
