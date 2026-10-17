*>--- conformance-case
*> id: pb499_table_value_by_section_2002
*> rules: [GR-13.7.4-5, GR-13.18.63.4-4, GR-13.18.63.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB499 - WHEN a FORMAT 2 (table) VALUE takes effect, by section. ISO 13.18.63.4 GR11 imports
      *> "General rules 1, 2, 3, 4, 5, 6, 7, 8, and 10 above" into format 2, so the format-2 carrier is governed
      *> by the same occasions as the format-1 one, and 14.9.20.4 GR5 c) 1. c) names the qualifying condition:
      *> "A table format VALUE clause is specified in the data description entry of the elementary item and that
      *> VALUE clause specifies a value for the particular occurrence of the elementary data item."
      *> The lane was once blind to that carrier entirely; PB418 routed the read through DataItem.ValueAt and
      *> this golden is the witness the rows were missing.
      *>
      *> Every value below is COMPUTED FROM THE RULES, not measured. The table is
      *> `OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2)` - 13.18.63.4 GR12-GR15 key "AA" to occurrence 1 and
      *> "BB" to occurrence 2, and occurrence 3 is NOT specified, which is the leg that tells a real
      *> per-occurrence map from a whole-table fill: GR5c1c qualifies only the occurrences the clause gives a
      *> value to, so occurrence 3 keeps whatever it held.
      *>
      *>   W  GR4 c) - an ordinary working-storage item takes its VALUE "when the object or runtime element is
      *>      placed in initial state, and during the execution of an INITIALIZE statement". So W starts
      *>      [AABB  ] (occurrence 3 is the category default, spaces), and after MOVE ALL "." + INITIALIZE it is
      *>      [AABB..] - the two keyed occurrences restored, the third left at the dots.
      *>   X  GR4 a) - "for external items, during the execution of an INITIALIZE statement", and NOT at initial
      *>      state. So X reads [      ] first and [AABB  ] only after the INITIALIZE.
      *>   B  GR4 b) - "for based items and their subordinate items, during the execution of an ALLOCATE or an
      *>      explicit or implicit INITIALIZE statement". ALLOCATE ... INITIALIZED gives [AABB  ].
      *>   P  GR4's closing paragraph - "When VALUE clauses take effect, data items with a VALUE clause are
      *>      initialized to the specified value and data items of class message-tag, class object, and class
      *>      pointer are initialized to null" - on the INITIALIZE occasion GR4 c) names, so a pointer holding a
      *>      live address is nulled by INITIALIZE ... ALL TO VALUE. 14.9.20.4 GR5 c) 1. a) is the qualification
      *>      (the CATEGORY, not a VALUE clause - 13.18.63.3 SR9 forbids one there) and GR6 a) 1. the sender.
      *> COBOL-2002 because the format-2 (table) VALUE, BASED and ALLOCATE are all post-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB499-BY-SECTION-2002.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W.
          05 WE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       01 X EXTERNAL.
          05 XE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       01 PTR USAGE POINTER.
       01 ANCHOR PIC X(3) VALUE "ABC".
       LINKAGE SECTION.
       01 B BASED.
          05 BE PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" FROM (1) TO (2).
       PROCEDURE DIVISION.
           DISPLAY "W-START=[" W "]"
           MOVE ALL "." TO W
           INITIALIZE W ALL TO VALUE
           DISPLAY "W-TOVAL=[" W "]"
           DISPLAY "X-START=[" X "]"
           INITIALIZE X ALL TO VALUE
           DISPLAY "X-TOVAL=[" X "]"
           ALLOCATE B INITIALIZED RETURNING PTR
           SET ADDRESS OF B TO PTR
           DISPLAY "B-ALLOC=[" B "]"
           SET PTR TO ADDRESS OF ANCHOR
           IF PTR = NULL DISPLAY "P-NULL" ELSE DISPLAY "P-SET" END-IF
           INITIALIZE PTR ALL TO VALUE
           IF PTR = NULL DISPLAY "P2-NULL" ELSE DISPLAY "P2-SET" END-IF
           STOP RUN.
