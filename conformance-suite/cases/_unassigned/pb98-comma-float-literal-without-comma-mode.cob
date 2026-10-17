*>--- conformance-case
*> id: pb98-comma-float-literal-without-comma-mode
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 12.3.7.4 GR14 a) / 8.3.3.3.2: the comma is the decimal separator of a numeric literal ONLY
      *> under DECIMAL-POINT IS COMMA - the floating-point form's significand included (8.3.3.3.3). Without the
      *> clause `1,5E+3` (one token since kb/Work PB98) is COBOLNET0895, in a VALUE clause and in a statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB98NCF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 USAGE FLOAT-LONG VALUE 1,5E+3.
       01 F2 USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
           MOVE 2,5E+2 TO F2.
           STOP RUN.
