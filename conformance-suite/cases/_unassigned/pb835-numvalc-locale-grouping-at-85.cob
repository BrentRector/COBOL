*>--- conformance-case
*> id: pb835-numvalc-locale-grouping-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB835 - the mon_grouping-checked NUMVAL-C LOCALE scan (ISO 15.68.3 r5b.6) below its
      *> introducing edition: the LOCALE keyword of NUMVAL-C is a COBOL-2002 construct (15.68.2), so at
      *> --std 85 the grouped monetary argument is refused at compile time rather than silently scanned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB835N85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C("1,234,567.89" LOCALE US)
           STOP RUN.
