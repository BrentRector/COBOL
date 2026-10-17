*>--- conformance-case
*> id: pb11-format-kind-chimera
*> rules: [AR-15.38.3-2, AR-15.40.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3.4 - a combined format is a date format, an uppercase T, and a time format, with BASIC paired
      *> to BASIC and EXTENDED to EXTENDED. "YYYY-MM-DDThhmmss" is an EXTENDED date joined to a BASIC time, so
      *> it is not a combined format at all - and every one of its subfields is individually legal, which is
      *> precisely why a character-wise check accepted it and returned a fabricated "1874-10-16T010000".
      *> This is the case that makes the fix a RECOGNIZER rather than more field checks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB11NEGCHIMERA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(30).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThhmmss" 100000 3600) TO T
           STOP RUN.
