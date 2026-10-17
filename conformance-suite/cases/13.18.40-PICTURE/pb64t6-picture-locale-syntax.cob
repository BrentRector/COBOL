*>--- conformance-case
*> id: pb64t6-picture-locale-syntax
*> rules: [SR-13.18.40.3-33, SR-13.18.40.3-35, SR-13.18.40.3-36]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR33-SR35 + 13.18.40.6 Table 11 - the format-2 picture syntax rules, one violating item
      *> per rule, each drawing COBOLNET1673 with the sub-rule named (kb/Work PB64 T6):
      *>   B1 SR33 - no 'Z' or '9' at all.  B2 Table 11 - a 'Z' follows a '9' (no 9 may precede any Z).
      *>   B3 SR35 - 32 digit positions.   B4 Table 11 - '+' is not the first symbol.
      *>   B5 Table 11 - the currency symbol after a digit.  B6 - a symbol outside {+ cs Z 9 .} (an 'X').
      *>   B7 - SIZE 0 (integer-1 gives the item's character positions, 13.18.40.4 GR17).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6NS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC +$ LOCALE IS FR SIZE IS 4.
       01 B2 PIC Z9.Z9 LOCALE IS FR SIZE IS 8.
       01 B3 PIC 9(32) LOCALE IS FR SIZE IS 40.
       01 B4 PIC ZZ+9 LOCALE IS FR SIZE IS 6.
       01 B5 PIC 9$9 LOCALE IS FR SIZE IS 6.
       01 B6 PIC ZX9 LOCALE IS FR SIZE IS 6.
       01 B7 PIC ZZ9 LOCALE IS FR SIZE IS 0.
       PROCEDURE DIVISION.
           STOP RUN.
