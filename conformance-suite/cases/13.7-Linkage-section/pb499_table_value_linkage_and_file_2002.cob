*>--- conformance-case
*> id: pb499_table_value_linkage_and_file_2002
*> rules: [GR-13.7.4-5, GR-13.18.63.4-3, GR-13.18.63.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB499 - the two sections whose VALUE clauses take effect ONLY at an INITIALIZE, carrying a
      *> FORMAT 2 (table) VALUE. ISO 13.18.63.4 GR11 imports general rules 1-8 and 10 into format 2, so these
      *> are the format-2 readings of GR3 and GR2; 14.9.20.4 GR5 c) 1. c) is the qualification and GR6 a) 3.
      *> the sender: "If the data item is a table element, the literal in the VALUE clause that corresponds to
      *> the occurrence being initialized determines the sending-operand."
      *>
      *> Every value below is COMPUTED FROM THE RULES. The table is `OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO
      *> (2)`: GR12-GR15 key "AA" to occurrence 1 and "BB" to occurrence 2 and leave occurrence 3 unspecified,
      *> so a correct INITIALIZE restores the first four characters and leaves the last two as they were - the
      *> leg that tells a per-occurrence map from a whole-table fill.
      *>
      *>   L  GR3 - "In the linkage section, VALUE clauses take effect only during the execution of an explicit
      *>      or implicit INITIALIZE statement." The caller passes an item it filled with dots, so the callee
      *>      reads [......] on entry - NOT the table values - and [AABB..] after INITIALIZE. The caller then
      *>      prints the same, because 14.9.20 operates on the argument itself (BY REFERENCE, 14.9.4.4).
      *>   R  GR2 - "In the file section, VALUE clauses take effect only during the execution of an INITIALIZE
      *>      statement. The initial value of the data items in the file section is undefined." The record is
      *>      given a known content first (MOVE ALL ".") precisely because the initial one is undefined and a
      *>      test may not assert it; INITIALIZE then gives [AABB..].
      *> COBOL-2002 because the format-2 (table) VALUE is post-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB499-LINK-FILE-2002.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb499-link-file.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 RE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       WORKING-STORAGE SECTION.
       01 A PIC X(6) VALUE "......".
       PROCEDURE DIVISION.
           CALL "PB499-LINK-FILE-SUB" USING A
           DISPLAY "CALLER=[" A "]"
           OPEN OUTPUT F
           MOVE ALL "." TO R
           DISPLAY "FILE-BEFORE=[" R "]"
           INITIALIZE R ALL TO VALUE
           DISPLAY "FILE-AFTER=[" R "]"
           CLOSE F
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB499-LINK-FILE-SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 LE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       PROCEDURE DIVISION USING L.
           DISPLAY "SUB-BEFORE=[" L "]"
           INITIALIZE L ALL TO VALUE
           DISPLAY "SUB-AFTER=[" L "]"
           GOBACK.
       END PROGRAM PB499-LINK-FILE-SUB.
       END PROGRAM PB499-LINK-FILE-2002.
