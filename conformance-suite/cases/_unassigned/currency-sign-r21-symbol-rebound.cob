*>--- conformance-case
*> id: currency-sign-r21-symbol-rebound
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 CURRENCY SIGN syntax rules, on the SET model (PB60 / AR-15.68.3-3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CURR21.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U"
           CURRENCY SIGN IS "UAH" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM CURR21.
