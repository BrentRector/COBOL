*>--- conformance-case
*> id: pb100-picture-locale-format-2
*> rules: [GR-12.3.7.4-5, SR-13.18.40.3-34]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.40.3 SR34 - each of '+', '.', the currency symbol may appear ONLY ONCE in a
      *> format 2 character-string: +$$9.99 carries two currency symbols (there is NO floating insertion in
      *> format 2 - the currency string's position, length and characters are the LOCALE's, 13.18.40.5 r9).
      *> COBOLNET1673 names the sub-rule. (kb/Work PB64 T6 - the fixture's former P1, `+$9.9 LOCALE SIZE IS
      *> 10`, is LEGAL format 2 and moved to the positive golden pb64t6_picture_locale_smoke; pre-T6 both
      *> drew the documented-non-support COBOLNET1518, deleted with the claim.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB100PL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr_FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P2 PICTURE IS +$$9.99 LOCALE IS FR SIZE 12.
       PROCEDURE DIVISION.
           STOP RUN.
