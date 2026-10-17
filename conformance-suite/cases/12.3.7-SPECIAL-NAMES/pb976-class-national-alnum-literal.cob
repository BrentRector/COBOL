*>--- conformance-case
*> id: pb976-class-national-alnum-literal
*> rules: [SR-12.3.7.3-17]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB976 - ISO 12.3.7.3 SR17 c) 3.: "When the NATIONAL phrase is specified: ... Each noninteger
*> literal shall be a national literal."  "0" and "9" are ALPHANUMERIC literals in a FOR NATIONAL
*> class.  The FOR phrase used to be parsed and ignored, so this compiled clean and SR17 was recorded
*> CONFORMS on evidence that never exercised it.  (Below 2002 the FOR phrase itself is the
*> version matrix's refusal.)  Expected: COBOLNET1671 naming SR17 c3.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB976NEG.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    CLASS HN FOR NATIONAL IS "0" THRU "9".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NX PIC N(2) VALUE N"12".
PROCEDURE DIVISION.
    IF NX IS HN DISPLAY "IN" END-IF
    STOP RUN.
