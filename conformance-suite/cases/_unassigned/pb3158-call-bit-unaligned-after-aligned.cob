*>--- conformance-case
*> id: pb3158-call-bit-unaligned-after-aligned
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR6: a BY REFERENCE bit item shall be described such that it is aligned on a byte
      *> boundary.  SUB begins at bit 2 of the record (8.5.1.6.3: a bit group item immediately following a
      *> same-level bit item takes the next bit position).  TA is ALIGNED (13.18.1.4 GR1: "aligned on the
      *> first bit of the first available byte boundary" - of the record, bit 8) and is ACCEPTED; UA follows
      *> TA's 5 bits at bit 13 and is the ONE refusal (kb/Work PB3158: the walk counted TA's byte boundary
      *> from SUB's start, bit 10, and refused TA as well).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3158N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 BA PIC 1(2) USAGE BIT.
          05 SUB GROUP-USAGE BIT.
             10 SA PIC 1 USAGE BIT.
             10 TA PIC 1(5) USAGE BIT ALIGNED.
             10 UA PIC 1(3) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING BY REFERENCE TA
           CALL "SUB" USING BY REFERENCE UA
           STOP RUN.
