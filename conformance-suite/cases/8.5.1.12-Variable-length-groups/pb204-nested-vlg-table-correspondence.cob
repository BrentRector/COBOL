*>--- conformance-case
*> id: pb204-nested-vlg-table-correspondence
*> rules: [GR-8.5.1.12.1-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.5.1.12.1 RULE 1 / 8.5.1.12.2 sentence 2 - the first of the three compatibility conditions
*> (kb/Work PB204). "For each dynamic-capacity table in either group there is a corresponding table in the
*> other group", and "two tables correspond if AT LEAST ONE OF THEM IS A DYNAMIC-CAPACITY TABLE and they
*> occupy the same relative byte positions within their groups". VT is a dynamic-capacity table at relative
*> byte position 2; LQ at that position is not a table at all, so no correspondence exists and 14.8.2.2
*> refuses the crossing. Rule 1's own witness, distinct from rule 2's (pb204-nested-vlg-element-width, where
*> the tables DO correspond and fail to MATCH) and rule 3's (pb204-nested-vlg-position).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          05 VF PIC X(2).
          05 VT OCCURS DYNAMIC CAPACITY IN VC FROM 1.
             10 VT-N PIC 9(2).
             10 VT-A PIC X.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB204N4S" AS NESTED USING VG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N4S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LVG.
          05 LF PIC X(2).
          05 LQ PIC X(3).
       PROCEDURE DIVISION USING LVG.
       S1.
           CONTINUE.
       END PROGRAM PB204N4S.
       END PROGRAM PB204N4.
