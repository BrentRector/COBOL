*>--- conformance-case
*> id: pb65-leap-second-within-unit
*> rules: [SR-7.3.17.3-1, AR-15.79.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 7.3.17.3 SR1: "The LEAP-SECOND directive shall not be specified within a compilation unit." A
      *> >>LEAP-SECOND after the first IDENTIFICATION DIVISION is inside the unit - COBOLNET2652 (kb/Work PB65; the
      *> placement rule is data on the directive's row, judged by DirectivePlacementPass since kb/Work PB1378 -
      *> it was COBOLNET1650 from the LEAP-SECOND stage's own latch).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65NLEAPIN.
       >>LEAP-SECOND ON
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(6).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960").
           STOP RUN.
