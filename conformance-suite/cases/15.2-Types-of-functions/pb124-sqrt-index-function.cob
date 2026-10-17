*>--- conformance-case
*> id: pb124-sqrt-index-function
*> rules: [GR-15.2-6, AR-15.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.84.3 r1 requires class NUMERIC; 15.2 item 6 makes MAX over index arguments an INDEX
      *> function, class index - which 8.5.2.1 Table 2 keeps apart from Numeric. The nested result's
      *> storage category folded to numeric and the screen admitted it (kb/Work PB124 wave 5b,
      *> AR-15.3-10's argument-side twin of the same fold).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX1 USAGE INDEX.
       01 IX2 USAGE INDEX.
       01 R PIC 9(9).
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION SQRT(FUNCTION MAX(IX1 IX2))
           STOP RUN.
