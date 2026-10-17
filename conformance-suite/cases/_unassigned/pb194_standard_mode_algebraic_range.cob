*>--- conformance-case
*> id: pb194_standard_mode_algebraic_range
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB194 - ARITHMETIC IS STANDARD (the COBOL-2002 mode, obsolete 2014, removed 2023) routes to
      *> the SAME SDIDI decimal engine as STANDARD-DECIMAL: its standard intermediate data item for every
      *> operand COBOL.NET can carry IS the standard-decimal one (8.8.1.5.2), so every mode-conditioned
      *> decision must answer identically for the two spellings.
      *>
      *> The IN-ARITHMETIC-RANGE screen of 15.43.4 r1 / 15.58.4 r1 (8.8.4.4.4 GR3 l) measures a floating-point
      *> numeric-edited argument-1's extreme against the intermediate data item's range for the arithmetic mode
      *> in effect.  PIC 9(3)E+999 reaches 1E+1002 - past binary64's 1.8E+308, INSIDE the SDIDI's 9.99E+6144.
      *> This program was REJECTED before PB194 ("COBOLNET1660 ... outside the native (binary64) intermediate's
      *> range") while the identical entry under ARITHMETIC IS STANDARD-DECIMAL compiled: the mode SET was
      *> written down in four places and two of the copies named STANDARD-DECIMAL alone.  It now lives once, in
      *> ArithmeticModes.IntermediateExponentRange.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB194SMR.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EA PIC 9(3)E+999.
       01 EB PIC -9(3)E+999.
       PROCEDURE DIVISION.
           DISPLAY "HI=" FUNCTION HIGHEST-ALGEBRAIC(EA).
           DISPLAY "LO=" FUNCTION LOWEST-ALGEBRAIC(EB).
           STOP RUN.
