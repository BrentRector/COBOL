*>--- conformance-case
*> id: pb64t1-set-locale-duplicate-category
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.2 Format 11: the category brace carries CHOICE INDICATORS - 5.2.6.4: "one or more of the
      *> alternatives ... any single alternative shall be specified only once ... in any order". LC_TIME twice is
      *> COBOLNET1666 (kb/Work PB64 T1; DESIGN-locale-facility 7 rule c). LC_NUMERIC LC_TIME (two different
      *> alternatives, any order) is the LEGAL multi-category form the positive golden pb64t1_set_locale_categories pins.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1DUPC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       PROCEDURE DIVISION.
           SET LOCALE LC_TIME LC_NUMERIC LC_TIME TO FR.
           STOP RUN.
