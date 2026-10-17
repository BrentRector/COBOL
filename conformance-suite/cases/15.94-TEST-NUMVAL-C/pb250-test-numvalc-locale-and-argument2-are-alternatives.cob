*>--- conformance-case
*> id: pb250-test-numvalc-locale-and-argument2-are-alternatives
*> rules: [FMT-15.94.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.94.2 + 5.2.6.2 (kb/Work PB250 - the stack-exclusion negative FMT-15.94.2 owed under its OWN name;
*> only the NUMVAL-C twin existed, at negative/pb60-numvalc-anycase-position.cob). The general format puts
*> `LOCALE [ locale-name-1 ]` and `argument-2` in one bracketed STACK, and 5.2.6.2 says brackets enclosing
*> alternatives mean "the syntax element contained within the brackets or one of the alternatives contained
*> within the brackets may be explicitly specified" - ONE of them, never both. Writing an explicit currency
*> string AND the LOCALE keyword is therefore not a legal shape of this format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB250STK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TEST-NUMVAL-C("USD1,234.56" "USD" LOCALE US) TO T
           DISPLAY "T=" T
           STOP RUN.
