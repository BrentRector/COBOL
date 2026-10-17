      *> reject-at: 2002 2014 2023
      *> kb/Work PB3287 - ISO 13.7.3 SR4: a LINKAGE item the procedure division may not reference (not a USING / RETURNING
      *> operand, not subordinate to one, not a redefinition or renaming of one, not BASED) may not be named in it at
      *> all.  `L-E (ALL)` as the argument of FUNCTION SUM (15.3 argument rule 14: "When ALL is specified as a subscript, the effect
      *> is as if each table element associated with that subscript position were specified") names the table L-E, so it is such a reference - COBOLNET2746, the verdict
      *> `MOVE L-E (1) TO WS-X` already draws.  The table(ALL) argument looked its table up by FindItem alone and never
      *> asked the screen, so this compiled clean and summed a record the program may not reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9(4).
       LINKAGE SECTION.
       01  L-TBL.
           05  L-E PIC 9 OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-X = FUNCTION SUM (L-E (ALL)).
           STOP RUN.
