*>--- conformance-case
*> id: pb64t6-picture-locale-sign
*> rules: [SR-13.16.3-19]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR19 - "If the LOCALE phrase of the PICTURE clause is specified, the SIGN clause shall not
      *> be specified" (a format-2 item's sign representation is the LOCALE's, 13.18.40.5 r13) - COBOLNET1674.
      *> The SCREEN twin is 13.17.3 SR9; a REPORT GROUP entry carries NO such rule (13.15.3 - kb/Work PB113's
      *> asserted non-arm).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6SG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC +Z9 LOCALE IS FR SIZE IS 6 SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
           STOP RUN.
