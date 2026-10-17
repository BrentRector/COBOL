*>--- conformance-case
*> id: currency-sign-r23-string-digit
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 CURRENCY SIGN syntax rules, on the SET model (PB60 / AR-15.68.3-3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CURR23.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "US1" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM CURR23.
