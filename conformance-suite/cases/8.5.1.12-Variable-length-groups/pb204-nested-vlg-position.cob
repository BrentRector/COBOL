*>--- conformance-case
*> id: pb204-nested-vlg-position
*> rules: [GR-8.5.1.12.1-3, SR-14.9.4.3-25]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.5.1.12.2, POSITIONAL CORRESPONDENCE - the NEGATIVE side of kb/Work PB204. 14.8.2.2 admits a
*> variable-length group across a Format-2 CALL only when the two sides are COMPATIBLE "as described in
*> 8.5.1.12", and 8.5.1.12.1 rule 3 requires that "for each dynamic-length elementary item in either group
*> there is a corresponding dynamic-length elementary item in the other group", correspondence being
*> "start at the same relative byte positions within their groups" (8.5.1.12.2).
*> VD starts at relative byte position 3 and LD at 4, so the groups are NOT compatible and the CALL is
*> rejected. The WITNESS matters: with the compatibility relation absent, EVERY such CALL was rejected and
*> a fixture could not tell an implemented rule from a blanket refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          05 VF PIC X(3).
          05 VD PIC X DYNAMIC LENGTH.
          05 VT PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB204N1S" AS NESTED USING VG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N1S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LVG.
          05 LF PIC X(4).
          05 LD PIC X DYNAMIC LENGTH.
          05 LT PIC X.
       PROCEDURE DIVISION USING LVG.
       S1.
           CONTINUE.
       END PROGRAM PB204N1S.
       END PROGRAM PB204N1.
