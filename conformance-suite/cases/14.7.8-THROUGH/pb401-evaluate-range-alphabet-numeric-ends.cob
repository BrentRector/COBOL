*>--- conformance-case
*> id: pb401-evaluate-range-alphabet-numeric-ends
*> rules: [GR-14.7.8-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR3 sentence 1 - "Alphabet-name-1 may be specified only when the literals or identifiers
      *> specified in the THROUGH phrase are of class alphabetic, alphanumeric, or national." A range whose ends
      *> are ARITHMETIC EXPRESSIONS is of class numeric (14.9.13.2 prints arithmetic-expression-3/-4 among the
      *> range-expression's operands, and 8.8.1.1 makes every constituent of an arithmetic expression numeric), so
      *> IN alphabet-name-1 over it names a collating sequence 14.7.8 rule 1 has no use for.
      *> ⛔ THIS COMPILED CLEAN BEFORE kb/Work PB401 and the range was evaluated as a STRING range under AL. The
      *> screen was already written (COBOLNET1998, kb/Work PB398) and could not fire: the range's class was read
      *> through a category reader that answered "no category" for a computed operand, which the comparison-class
      *> rule reads as the ALPHANUMERIC arm. This case is the witness that the class read reaches the screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB401-RNG-ALPHA-NUM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9(3) VALUE 005.
       01 WS-A   PIC 9(3) VALUE 001.
       01 WS-B   PIC 9(3) VALUE 008.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-N
               WHEN WS-A + 1 THRU WS-B + 2 IN AL DISPLAY "IN"
               WHEN OTHER                        DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
