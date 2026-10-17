*>--- conformance-case
*> id: pb60_currency_sign_bare_implied_dollar
*> rules: [SR-12.3.7.3-22, SR-12.3.7.3-25, AR-15.68.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 (AR-15.68.3-3) - the bare CURRENCY SIGN "#" clause: 12.3.7.3 r22 makes '#' both string and symbol,
      *> and r25 still IMPLIES the '$' clause (no clause names '$'), so PIC ##,##9.99 edits with '#' (FH) AND
      *> PIC $$,$$9.99 stays legal and edits with '$' (FDL) - the former single-symbol model rejected the '$'
      *> picture as COBOLNET0808. NUMVAL-C without argument-2 (15.68.3 r3): the unit's ONE explicitly specified
      *> currency string is "#" - "#1,234.56" values 1234.56 and "$1,234.56" does not conform (EC default 0).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60CURBARE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FH   PIC ##,##9.99.
       01 FDL   PIC $$,$$9.99.
       01 R    PIC S9(9)V99.
       PROCEDURE DIVISION.
           MOVE 1234.5 TO FH.
           DISPLAY "FH=[" FH "]".
           MOVE 1234.5 TO FDL.
           DISPLAY "FDL=[" FDL "]".
           COMPUTE R = FUNCTION NUMVAL-C("#1,234.56").
           DISPLAY "NVC-HASH=" R.
           COMPUTE R = FUNCTION NUMVAL-C("$1,234.56").
           DISPLAY "NVC-DOLLAR=" R.
           STOP RUN.
       END PROGRAM PB60CURBARE.
