*>--- conformance-case
*> id: pb64t6_numvalc_locale
*> rules: [AR-15.68.3-5, RV-15.68.4-3, AR-15.94.3-1, RV-15.94.4-1,
*>    FMT-15.94.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB64 T6 — the NUMVAL-C / TEST-NUMVAL-C LOCALE arm (ISO 15.68.3 r5, 15.68.4 r1-r3, 15.94.4; A.4.9 item
      *> 12). The CURRENT locale is the harness-pinned INVARIANT (currency U+00A4, decimal '.', grouping ',',
      *> CurrencyNegativePattern 0 - the PARENTHESES are the negative convention and a bare '-' is a character in
      *> error); US is en-US (int_curr_symbol "USD" - r5b.3 matches its first three characters; ANYCASE folds
      *> ONLY the currency string, r5b.1). Every verdict is a numeric compare or an ASCII display.
      *>   CUR/PARENS - r5 under the current locale; 15.68.4 r3's sign contract (parens negative, '-' in error).
      *>   INTCURR/ANYCASE - r5b.3's international form, with and without the case fold.
      *>   POS - 15.94.4 r1 b.1's own worked example: "0 1" reports position 3.
      *>   NEG1 - "-1,234.56" under the parens convention: position 1.
      *>   MISSING - a DECLARED locale no environment provides raises EC-LOCALE-MISSING AT USE (8.2.1;
      *>   checking on - the declarative observes it and the COMPUTE is interrupted).
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6NV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US"
           LOCALE XX IS "xx-NOWHERE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(7)V99.
       01 T PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE R = FUNCTION NUMVAL-C("1,234.56" LOCALE)
           IF R = 1234.56 DISPLAY "CUR OK" ELSE DISPLAY "CUR BAD " R END-IF
           COMPUTE R = FUNCTION NUMVAL-C("(1,234.56)" LOCALE)
           IF R = -1234.56 DISPLAY "PARENS OK" ELSE DISPLAY "PARENS BAD " R END-IF
           COMPUTE R = FUNCTION NUMVAL-C("USD 1,234.56" LOCALE US)
           IF R = 1234.56 DISPLAY "INTCURR OK" ELSE DISPLAY "INTCURR BAD " R END-IF
           COMPUTE R = FUNCTION NUMVAL-C("usd1,234.56" LOCALE US ANYCASE)
           IF R = 1234.56 DISPLAY "ANYCASE OK" ELSE DISPLAY "ANYCASE BAD " R END-IF
           MOVE FUNCTION TEST-NUMVAL-C("0 1" LOCALE) TO T
           IF T = 3 DISPLAY "POS OK" ELSE DISPLAY "POS BAD " T END-IF
           MOVE FUNCTION TEST-NUMVAL-C("-1,234.56" LOCALE) TO T
           IF T = 1 DISPLAY "NEG1 OK" ELSE DISPLAY "NEG1 BAD " T END-IF
           MOVE FUNCTION TEST-NUMVAL-C("usd1,234.56" LOCALE US) TO T
           IF T = 1 DISPLAY "CASE OK" ELSE DISPLAY "CASE BAD " T END-IF
           COMPUTE R = FUNCTION NUMVAL-C("1.23" LOCALE XX)
           DISPLAY "MISSING DONE"
           STOP RUN.
