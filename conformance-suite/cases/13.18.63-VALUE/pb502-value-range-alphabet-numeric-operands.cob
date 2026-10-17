*>--- conformance-case
*> id: pb502-value-range-alphabet-numeric-operands
*> rules: [GR-13.18.63.4-18]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR31 sentence 1: "Alphabet-name-1 may be specified only when the literals specified
      *> in the THROUGH phrase are of class alphanumeric or national." A NUMERIC condition-name range is
      *> ordered by 14.7.8 rule 1 - "when the range of values is defined by numeric literals, the range of
      *> values includes literal-1, literal-2, and all algebraic values between literal-1 and literal-2" -
      *> which names no collating sequence at all, so there is nothing for the phrase to select.
      *> The VALUE-clause arm of the screen pb398-range-alphabet-numeric-operands pins on EVALUATE
      *> (14.9.13.3 SR3 is the same rule for the EVALUATE range); kb/Work PB502 - only the EVALUATE arm had
      *> a witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB502NEG2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 3.
           88 N-RANGE VALUE 1 THRU 9 IN AL.
       PROCEDURE DIVISION.
       MAIN-P.
           IF N-RANGE DISPLAY "IN" ELSE DISPLAY "OUT" END-IF
           STOP RUN.
