*>--- conformance-case
*> id: numval-c-ambiguous-currency
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.68.3 r3: "If neither argument-2 nor the LOCALE keyword is
      *> specified, there shall be only one currency string for the compilation
      *> unit, either the default currency sign or a currency string specified in
      *> the SPECIAL-NAMES paragraph." Two clauses, two strings, no argument-2 -
      *> the former single-symbol model injected whichever clause bound LAST and
      *> NUMVAL-C("#1,234.56") silently returned 0 (PB60 / AR-15.68.3-3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60CURNEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "#"
           CURRENCY SIGN IS "@".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C("#1,234.56").
           DISPLAY "NVC=" R.
           STOP RUN.
       END PROGRAM PB60CURNEG.
