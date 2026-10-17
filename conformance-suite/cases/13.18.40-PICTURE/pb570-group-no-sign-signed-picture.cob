*>--- conformance-case
*> id: pb570-group-no-sign-signed-picture
*> rules: [SR-13.18.40.3-31]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR31 - "The symbol 'S' shall not be specified in character-string-1 when the NO SIGN
      *> phrase of the USAGE Clause is specified for the subject of the entry", restated by 13.18.60.4 GR11
      *> ("The PICTURE character string of the data item shall not contain the symbol 'S'").
      *> The phrase here is written at the GROUP level, and 13.18.60.4 GR1 applies that clause "only to each
      *> elementary item in the group" - so W1 is the subject the NO SIGN phrase is specified for, and its
      *> PICTURE contains 'S'. The elementary spelling `01 W1 PIC S9(4) PACKED-DECIMAL WITH NO SIGN.` has drawn
      *> COBOLNET1566 since the phrase landed; this, its only other legal spelling, compiled CLEAN at every
      *> edition and laid W1 out with the signed 3-byte representation (kb/Work PB570).
      *> Rejected at all four editions: below 2023 the phrase ALSO draws its introduction gate (COBOLNET0900),
      *> but SR31 is edition-independent and fires alongside it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB570NEGSIGNED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GS USAGE PACKED-DECIMAL WITH NO SIGN.
           05  W1  PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W1
           STOP RUN.
