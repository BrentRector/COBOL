*>--- conformance-case
*> id: pb70-refmod-leaf-under-strong-group
*> rules: [SR-8.4.3.3.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 SR1: "a numeric data item of usage display or national that is not subordinate to a
      *> strongly-typed group item" - the numeric leaf of a STRONG typed group is excluded (an alphanumeric leaf
      *> under the same group is admitted: bullet 3 carries no such restriction). kb/Work PB70: COBOLNET1647.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70NSTRONGLEAF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST-T TYPEDEF STRONG.
          05 SA PIC X(2).
          05 SB PIC 9(3).
       01 ST TYPE ST-T.
       01 R PIC X(4).
       PROCEDURE DIVISION.
           MOVE SA OF ST (1:1) TO R.
           MOVE SB OF ST (1:1) TO R.
           STOP RUN.
