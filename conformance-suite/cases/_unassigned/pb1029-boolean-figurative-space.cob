*>--- conformance-case
*> id: pb1029-boolean-figurative-space
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1029 - ISO 8.8.2: the figurative constants a boolean expression may be are "the
      *> figurative constant ZERO (ZEROS, ZEROES)" and "the figurative constant ALL literal, where
      *> literal is a boolean literal". SPACE is neither; the refusal was bound as a boolean error node
      *> WITHOUT a diagnostic, so the program compiled clean and aborted the run unit at the COMPUTE.
      *> Expected: COBOLNET1511 (boolean operand) at every edition (COBOL-85 also draws its 0900 gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1029NBF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC 1(4) USAGE BIT VALUE B"1010".
       PROCEDURE DIVISION.
           COMPUTE WS-B = WS-B B-AND SPACE
           STOP RUN.
