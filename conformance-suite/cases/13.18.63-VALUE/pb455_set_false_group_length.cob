*>--- conformance-case
*> id: pb455_set_false_group_length
*> rules: [GR-13.18.63.4-20, SR-14.9.39.3-7, GR-14.9.39.4-7,
*>    FMT-13.18.63.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! GR6/GR7's TAIL: A GROUP CONDITIONAL VARIABLE'S LENGTH IS THE OCCURS CLAUSE'S, AND A
      *> ZERO-LENGTH ONE IS LEFT UNCHANGED.  kb/Work PB455 / PB561.
      *>
      *> ISO 14.9.39.4 GR7: "If the FALSE phrase is specified, the literal in the FALSE phrase of the
      *> VALUE clause associated with condition-name-1 is placed in the conditional variable according
      *> to the rules for the VALUE clause, except that when the conditional variable is an
      *> alphanumeric group item, bit group item, or national group item to which a table is
      *> subordinate, its length is determined as specified in 13.18.38, OCCURS clause. If the length
      *> of the conditional variable is zero, the SET statement leaves it unchanged."  GR6 states the
      *> SAME two sentences for the TRUE phrase, which is why one program witnesses both arms.
      *>
      *> Expected values, COMPUTED FROM THE STANDARD (not measured):
      *>   13.18.38.3 SR16 admits integer-1 = 0, so G's length runs 0..4 characters with N.
      *>   N = 4 -> the conditional variable is 4 characters; GR6 places literal-2 "AAAA" by the VALUE
      *>            clause's rules (13.18.63.3 SR4, an alphanumeric group item) -> G = "AAAA", and
      *>            8.8.4.5.3 GR2's relation over 8.8.4.2.7 makes G-ON TRUE.
      *>   N = 2 -> the conditional variable is 2 characters; GR7 places literal-4 "BBBB" by those same
      *>            rules, so the group receives it left-justified with the excess truncated on the
      *>            right (14.9.25.4 GR4) -> G = "BB".
      *>   N = 0 -> the length IS zero, so GR6's last sentence leaves the conditional variable
      *>            unchanged; restoring N = 2 must therefore still show the "BB" the FALSE store left.
      *>
      *> The FALSE phrase and SET condition-name TO FALSE are a COBOL-2002 introduction (constructs.json
      *> rows value-false-phrase-2002 / set-condition-false-2002), which is why this program lives in
      *> the 2002 corpus; negative/pb555-set-false-below-2002 pins the edge below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB455GRPL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N               PIC 9 VALUE 4.
       01  G.
           88  G-ON        VALUE "AAAA" WHEN SET TO FALSE "BBBB".
           05  E           PIC X OCCURS 0 TO 4 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 4 TO N
           SET G-ON TO TRUE
           DISPLAY "T4=[" G "]"
           IF G-ON DISPLAY "ON4=yes" ELSE DISPLAY "ON4=no" END-IF
           MOVE 2 TO N
           SET G-ON TO FALSE
           DISPLAY "F2=[" G "]"
           IF G-ON DISPLAY "ON2=yes" ELSE DISPLAY "ON2=no" END-IF
           MOVE 0 TO N
           SET G-ON TO TRUE
           MOVE 2 TO N
           DISPLAY "Z2=[" G "]"
           MOVE 0 TO N
           SET G-ON TO FALSE
           MOVE 2 TO N
           DISPLAY "Z3=[" G "]"
           DISPLAY "DONE"
           STOP RUN.
