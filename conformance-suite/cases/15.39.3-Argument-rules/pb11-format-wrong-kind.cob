*>--- conformance-case
*> id: pb11-format-wrong-kind
*> rules: [AR-15.39.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.39.3 r2 - "The content of argument-1 shall be a date format." "hhmmss" is a legal TIME format
      *> (15.3.3.1) and therefore not a date format, so FORMATTED-DATE shall reject it. Before the recognizer
      *> it was accepted and the function returned "000000" - a fabricated value, not an error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB11NEGKIND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(30).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-DATE("hhmmss" 100000) TO T
           STOP RUN.
