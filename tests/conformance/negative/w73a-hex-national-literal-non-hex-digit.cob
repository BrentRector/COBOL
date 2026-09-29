      *> reject-at: 2002 2014 2023
      *> kb/Work PB1441 - ISO 8.3.3.5.3 SR4: "Hex-character-sequence-1
      *>  shall be composed of hexadecimal digits". NX" is a
      *>  three-character opening delimiter (8.3.5 rule 5). Before the
      *>  fix NX"00G1" re-lexed as the data-name NX followed by "00G1",
      *>  so MAX(NX"00G1") took the item NX and printed [ZZZZ] with no
      *>  diagnostic. COBOLNET2630.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGW73ANX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NX PIC X(4) VALUE "ZZZZ".
       01 NA PIC N(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION MAX(NX"00G1") TO NA.
           DISPLAY "[" NA "]".
           STOP RUN.
