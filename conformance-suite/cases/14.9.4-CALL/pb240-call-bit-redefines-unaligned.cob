*>--- conformance-case
*> id: pb240-call-bit-redefines-unaligned
*> rules: [SR-14.9.4.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB240 - ISO 14.9.4.3 SR6: a BY REFERENCE bit item "shall be
      *> described such that it is aligned on a byte boundary". RB2 is
      *> reached through a REDEFINES view: R starts at the first bit of X
      *> (13.18.44.4 GR1) and RB2 follows the same-level 3-bit RB1, sharing
      *> its byte (8.5.1.6.3 rule 1), so RB2 starts at bit 3. Before PB240
      *> the screen could not walk a REDEFINES view and ACCEPTED it unproven
      *> (COBOLNET1683 now).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 X PIC X(4).
          05 R REDEFINES X.
             10 RB1 PIC 1(3) USAGE BIT.
             10 RB2 PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB240NBS" USING BY REFERENCE RB2
           STOP RUN.
