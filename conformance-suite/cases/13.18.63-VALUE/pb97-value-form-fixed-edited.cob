*>--- conformance-case
*> id: pb97-value-form-fixed-edited
*> rules: [SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.63.3 SR6: "literals for fixed-point formats shall be specified as fixed-point" - a
      *> floating-point literal (8.3.3.3.3) on a FIXED-POINT numeric-edited item is COBOLNET1659, and a
      *> floating-point ZERO (0.0E+0) is not among the zero forms SR6 admits for either format (the figurative
      *> ZERO and the integer / decimal literal zero are). kb/Work PB97; the same rule reaches the level-88 form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB97NVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE1 PIC ZZ9.99 VALUE 1.5E+2.
       01 NE2 PIC ZZ9.99 VALUE 0.0E+0.
       01 NE3 PIC ZZ9.99.
          88 NE3-BIG VALUE 1.5E+2.
       PROCEDURE DIVISION.
           STOP RUN.
