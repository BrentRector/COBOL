*>--- conformance-case
*> id: pb204-nested-returning-incompatible
*> rules: [GR-8.5.1.12.1-3, SR-13.7.3-3, SR-14.9.4.3-25]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.4.3 SR25 -> 14.8.3.2, THE RETURNING HALF (kb/Work PB204; COBOLNET1736's witness). SR25 makes
*> "the rules for conformance specified in 14.8.2, Parameters and 14.8.3, Returning items" apply to a
*> Format-2 CALL, and 14.8.3.2 says: "If either the sending or the receiving operand is a variable length
*> group, the sending operand and the receiving operand shall be compatible, as described in 8.5.1.12."
*> The callee's returning item puts its dynamic-length member at relative byte position 2 and the CALL's
*> RETURNING identifier puts its own at 3, so 8.5.1.12.2 finds no correspondence. Before PB204 NO 14.8.3
*> conformance check existed for an AS NESTED call at all - the pair simply reached the emitter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RG.
          05 RP PIC X(3).
          05 RQ PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB204N3S" AS NESTED RETURNING RG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N3S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 SG.
          05 SP PIC X(2).
          05 SQ PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION RETURNING SG.
       S1.
           CONTINUE.
       END PROGRAM PB204N3S.
       END PROGRAM PB204N3.
