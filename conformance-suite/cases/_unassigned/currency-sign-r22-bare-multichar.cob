*>--- conformance-case
*> id: currency-sign-r22-bare-multichar
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 CURRENCY SIGN syntax rules, on the SET model (PB60 / AR-15.68.3-3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CURR22.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM CURR22.
