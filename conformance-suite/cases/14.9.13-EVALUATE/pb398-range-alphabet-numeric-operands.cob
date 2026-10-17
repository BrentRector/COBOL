*>--- conformance-case
*> id: pb398-range-alphabet-numeric-operands
*> rules: [SR-14.9.13.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR3, sentence 1: "Alphabet-name-1 may be specified only when the literals or identifiers
      *> specified in the THROUGH phrase are of class alphabetic, alphanumeric, or national." A NUMERIC range is
      *> ordered algebraically by 14.7.8 rule 1 - "the range of values includes literal-1, literal-2, and all
      *> algebraic values between literal-1 and literal-2" - and no collating sequence takes part in it, so
      *> naming one has no meaning. kb/Work PB398: this rule had no code site at all, because the phrase it
      *> constrains could not be written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398NEG3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-N
               WHEN 1 THRU 9 IN AL DISPLAY "IN"
               WHEN OTHER          DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
