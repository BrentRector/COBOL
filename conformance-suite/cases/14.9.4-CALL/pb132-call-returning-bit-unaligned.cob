*>--- conformance-case
*> id: pb132-call-returning-bit-unaligned
*> rules: [SR-14.9.4.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR8: a CALL RETURNING bit item shall be aligned on a byte boundary - the SAME
      *> chokepoint as SR6's argument screen, exercised through its isReturning arm (kb/Work PB132).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 A PIC 1(3) USAGE BIT.
          02 B PIC 1(5) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" RETURNING B
           STOP RUN.
