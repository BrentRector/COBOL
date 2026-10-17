*>--- conformance-case
*> id: numvalf_decimal_comma_and_spaces
*> rules: [AR-15.69.3-4, AR-15.69.3-5, RV-15.69.4-1, FMT-15.69.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.69 NUMVAL-F — two argument-side rules that need DISTINCT source units:
      *> NUMVALFSPC (no DECIMAL-POINT IS COMMA) pins §15.69.4 r1 — "Leading, trailing, and embedded
      *> spaces are ignored" — with every space sitting in a slot the §15.69.3 r1 format licenses
      *> (after the sign, after E, around the exponent sign and n): " + 1.5E + 1 " is +1.5×10¹ = 15.
      *> ⚠ The exponent SIGN is REQUIRED: the §15.69.3 r1 format (verified on printed page 898) puts
      *> the exponent's +/− in BRACES (a required choice), unlike the OPTIONAL square-bracket leading
      *> sign — "2.5E2" is not a legal argument, so every exponent below is written signed.
      *> DPCPROG (DECIMAL-POINT IS COMMA) pins §15.69.3 r4 — "the character comma shall be used in
      *> argument-1 instead of the character period to represent the decimal separator" — so "1,5E+1"
      *> is 15 and "2,25E+2" is 225. Integer receivers keep the output free of separator editing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMVALFSPC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R3  PIC 999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION NUMVAL-F(" + 1.5E + 1 ") TO R3
           DISPLAY "S1=" R3
           MOVE FUNCTION NUMVAL-F("  2.5E+2  ") TO R3
           DISPLAY "S2=" R3
           CALL "DPCPROG"
           STOP RUN.
       END PROGRAM NUMVALFSPC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DPCPROG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R3  PIC 999.
       PROCEDURE DIVISION.
       DP.
           MOVE FUNCTION NUMVAL-F("1,5E+1") TO R3
           DISPLAY "D1=" R3
           MOVE FUNCTION NUMVAL-F("2,25E+2") TO R3
           DISPLAY "D2=" R3
           GOBACK.
       END PROGRAM DPCPROG.
