*>--- conformance-case
*> id: pb886_refmod_dynamic_length_positions
*> rules: [GR-14.9.25.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB886 - the DYNAMIC LENGTH arm of the same rule (its own file because the construct is a
      *> COBOL-2014 introduction and cannot be written in the 2002 case). Every expected line is derived from
      *> the rule text, never from a run.
      *>
      *> ISO 14.9.25.4 GR1 - "MOVE a (b) TO b, c (b) / is equivalent to: / MOVE a (b) TO temp / MOVE temp TO b /
      *>   MOVE temp to c (b) / where 'temp' is an intermediate result item provided by the implementor." A
      *>   RESULT equivalence: the one-receiver and two-receiver forms owe the same value.
      *> ISO 8.5.1.10.4 - "A dynamic-length elementary item that is used as a sending operand or is
      *>   reference-modified is treated as a fixed-length data item whose length is the dynamic-length
      *>   elementary item's current length."
      *> ISO 8.4.3.3.4 GR5 b)/c) - leftmost-position and length are bounded by "the number of positions in the
      *>   data item referenced by identifier-1", which for this item is that current length; 13.18.19.4 GR2's
      *>   LIMIT phrase is its ceiling.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> DYN-ONE / DYN-TWO      DL holds "ABCDEFGH", so its current length is 8 and DL(2:5) is "BCDEF" - five
      *>                        character positions. Moved to a PIC X(6) receiver, 14.6.8's alignment pads one
      *>                        space on the right: "BCDEF ". Both forms owe it. An intermediate sized by the
      *>                        item's STATIC character occupancy - which is none, 8.5.1.10 - collapses to one
      *>                        position and gives "B     ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB886DYN14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DL             PIC X DYNAMIC LENGTH LIMIT 20.
       01 D1             PIC X(6).
       01 D2             PIC X(6).
       PROCEDURE DIVISION.
           MOVE "ABCDEFGH" TO DL
           MOVE DL(2:5) TO D1
           DISPLAY "DYN-ONE=[" D1 "]"
           MOVE SPACES TO D1
           MOVE DL(2:5) TO D1 D2
           DISPLAY "DYN-TWO=[" D1 "][" D2 "]"
           STOP RUN.
