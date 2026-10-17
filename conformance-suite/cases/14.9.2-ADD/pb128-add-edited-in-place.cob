*>--- conformance-case
*> id: pb128-add-edited-in-place
*> rules: [SR-14.9.2.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.2.3 SR2: ADD's in-place TO receivers shall reference NUMERIC data items - SR4 grants
      *> numeric-edited to the GIVING identifier-3 ONLY. ADD 1 TO ZZZ9 bound clean and stored through the
      *> EditMask arm with no diagnostic (kb/Work PB128; batch 8's SR-14.9.2.3-2 find).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB128NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED PIC ZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO ED
           STOP RUN.
