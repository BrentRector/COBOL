*>--- conformance-case
*> id: pb177-redefines-dynamic-subordinate
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The OTHER side of the COBOLNET1525 -> COBOLNET1698 RECODE, which had no witness at all: a REDEFINES
      *> subject with a DYNAMIC-CAPACITY TABLE subordinate to it. 8.5.1.12.1 makes that a "variable-length
      *> group" - "a group item whose data description has at least one dynamic-length elementary item or
      *> dynamic-capacity table as a subordinate item" - so 13.18.44.3 SR17 rejects it: "Neither data-name-2 nor
      *> the subject of the entry shall be a variable-length group or a dynamic-length elementary item."
      *> ⛔ IT IS THE dynamic-CAPACITY DISJUNCT OF ReferenceResolver.HasVariableLengthSubordinate, WHICH NO
      *> FIXTURE ANYWHERE EXERCISED: every other 8.5.1.12.1 rejection fixture reaches that predicate through
      *> `PIC X DYNAMIC LENGTH`, so half of a two-armed predicate was unwitnessed while the recode moved this
      *> very shape from 1525 to 1698.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB177NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(20).
       01 B REDEFINES A.
          05 T PIC X(3) OCCURS DYNAMIC FROM 1 TO 5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.
