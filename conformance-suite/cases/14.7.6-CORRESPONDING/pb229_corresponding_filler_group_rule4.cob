      *>--- conformance-case
      *> id: pb229_corresponding_filler_group_rule4
      *> rules: [GR-14.7.6-1, GR-14.7.6-4, GR-14.7.6-5, GR-14.9.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB229 - ISO 14.7.6 rules 1, 4 and 5 for ADD, SUBTRACT
      *>   and MOVE CORRESPONDING, derived from the rules, not a run.
      *> cite.py --check 14.7.6 "are not implicitly or explicitly
      *>   described with the keyword FILLER and have the same
      *>   data-name and the same qualifiers, if any, up to, but not
      *>   including, D1 and D2" -> OK 14.7.6 1)
      *> cite.py --check 8.4.2.2.1 "Qualification is the specification
      *>   of superordinate names from the hierarchy to which a
      *>   user-defined name belongs. The superordinate names are
      *>   called qualifiers." -> OK 8.4.2.2.1
      *> cite.py --check 13.18.20.4 "Under no circumstances shall a
      *>   FILLER item be referred to explicitly." -> OK 13.18.20.4 1)
      *> cite.py --check 14.7.6 "Neither data item contains an OCCURS,
      *>   REDEFINES, or RENAMES clause" -> OK 14.7.6 4)
      *> cite.py --check 14.7.6 "Neither data item is subordinate to a
      *>   group item that is subordinate to D1 or D2 when the group
      *>   item contains an OCCURS or REDEFINES clause" -> OK 14.7.6 5)
      *> RULE 1 excludes an item DESCRIBED with FILLER. An item UNDER a
      *> FILLER group is not one, and a FILLER group has no name, so it
      *> is no qualifier: F1 and F2 (under one and two FILLER levels)
      *> have no qualifier up to G1, as F1 and F2 have none up to G2;
      *> P1 has the one qualifier PG on both sides. All three pair
      *> (docs/CONFORMANCE.md 3 D-CORRFILLER). Every leg can fail:
      *>   K    plain on both sides           -> pair (control)
      *>   TA   G1's TA has OCCURS 2          -> rule 4, no pair
      *>   TB   G2's TB has OCCURS 2          -> rule 4, no pair
      *>   RA   G1's RA has REDEFINES         -> rule 4, no pair
      *>   F1   G1: under a FILLER group      -> pair
      *>   F2   under FILLER groups both sides -> pair
      *>   P1   PG.FILLER.P1 / PG.P1          -> pair
      *>   X1   under a FILLER OCCURS 2 group -> rule 5, no pair
      *>   X2   under a FILLER REDEFINES group -> rule 5, no pair
      *> Start: every G1 item 1, every G2 item 10.
      *>   ADD CORRESPONDING G1 TO G2: K F1 F2 P1 = 10 + 1 = 11, the
      *>     rest stay 10.
      *>   SUBTRACT CORRESPONDING G1 FROM G2: the same four, 11 - 1 = 10.
      *>   Every G1 item 7, MOVE CORRESPONDING G1 TO G2: the same four
      *>     become 7 (rule 2: a numeric to numeric move is valid).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB229FILLER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 K PIC 9(3).
          05 TA PIC 9(3) OCCURS 2.
          05 TB PIC 9(3).
          05 RB PIC 9(3).
          05 RA REDEFINES RB PIC 9(3).
          05 FILLER.
             10 F1 PIC 9(3).
             10 FILLER.
                15 F2 PIC 9(3).
          05 PG.
             10 FILLER.
                15 P1 PIC 9(3).
          05 FILLER OCCURS 2.
             10 X1 PIC 9(3).
          05 XB PIC 9(3).
          05 FILLER REDEFINES XB.
             10 X2 PIC 9(3).
          05 FILLER PIC 9(3).
       01 G2.
          05 K PIC 9(3).
          05 TA PIC 9(3).
          05 TB PIC 9(3) OCCURS 2.
          05 RA PIC 9(3).
          05 F1 PIC 9(3).
          05 FILLER.
             10 F2 PIC 9(3).
          05 PG.
             10 P1 PIC 9(3).
          05 X1 PIC 9(3).
          05 X2 PIC 9(3).
       01 V PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO V.
           PERFORM SET-G1.
           MOVE 10 TO K OF G2 TA OF G2 TB OF G2 (1) TB OF G2 (2)
                      RA OF G2 F1 OF G2 F2 OF G2 P1 OF G2
                      X1 OF G2 X2 OF G2.
           ADD CORRESPONDING G1 TO G2.
           PERFORM SHOW.
           SUBTRACT CORRESPONDING G1 FROM G2.
           PERFORM SHOW.
           MOVE 7 TO V.
           PERFORM SET-G1.
           MOVE CORRESPONDING G1 TO G2.
           PERFORM SHOW.
           STOP RUN.
       SET-G1.
           MOVE V TO K OF G1 TA OF G1 (1) TA OF G1 (2) TB OF G1
                     RB OF G1 F1 OF G1 F2 OF G1 P1 OF G1
                     X1 OF G1 (1) X1 OF G1 (2) X2 OF G1.
       SHOW.
           DISPLAY "K=" K OF G2 " TA=" TA OF G2
                   " TB=" TB OF G2 (1) "," TB OF G2 (2)
                   " RA=" RA OF G2 " F1=" F1 OF G2 " F2=" F2 OF G2
                   " P1=" P1 OF G2 " X1=" X1 OF G2 " X2=" X2 OF G2.
