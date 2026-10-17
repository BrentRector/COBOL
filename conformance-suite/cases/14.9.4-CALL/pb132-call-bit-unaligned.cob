*>--- conformance-case
*> id: pb132-call-bit-unaligned
*> rules: [SR-14.9.4.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR6: a BY REFERENCE bit item shall be described such that it is aligned on a byte
      *> boundary. B follows a same-level 3-bit item (8.5.1.6.3 rule 1 - it SHARES the byte), so it starts
      *> at bit 3 (kb/Work PB132; the 8.5.1.6.3 cursor walk is the one law - BitLayout.StartBitWithin).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 A PIC 1(3) USAGE BIT.
          02 B PIC 1(5) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING B
           STOP RUN.
