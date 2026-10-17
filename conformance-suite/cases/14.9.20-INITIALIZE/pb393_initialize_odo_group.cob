      *>--- conformance-case
      *> id: pb393_initialize_odo_group
      *> rules: [GR-14.9.20.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB393. ISO 1989:2023 14.9.20.4 GR8: "If identifier-1 references a group data item, affected
      *> elementary data items are initialized in the sequence of their definition within the group data item.
      *> For a variable-occurrence data item, the number of occurrences initialized is determined by the rules
      *> of the OCCURS clause for a receiving data item." identifier-1 IS a receiving operand (14.9.20.3 SR7),
      *> so 13.18.38.4 GR8 decides, and it decides DIFFERENTLY per quadrant:
      *>   GR8a - data-name-1 OUTSIDE the group: "only that part of the table area that is specified by the
      *>          value of the data item referenced by data-name-1 at the start of the operation will be used".
      *>          G-OUT depends on CNT = 3, so occurrences 1..3 are initialized (to the 14.9.20.4 GR6c
      *>          alphanumeric default, SPACES); 4 and 5 are outside the operation and 13.18.38.4 GR7 makes
      *>          their content undefined, so this fixture does not read them.
      *>   GR8b - data-name-1 INSIDE the group and the group receiving: "the maximum length of the group will
      *>          be used". G-IN depends on GI-N, which is subordinate to it, so ALL FIVE occurrences are
      *>          initialized even though GI-N held 2 - which is what B= proves by reading 4 and 5. GI-N itself
      *>          is an affected elementary item of category numeric and takes GR6c's ZEROES, shown by N=.
      *> Before this fixture an occurs-depending group resolved to an OdoGroupPlace - a decorator over the
      *> member place - which INITIALIZE's storage-form switch met in its `_ => null` arm: this whole program
      *> compiled clean and ABORTED the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393INIODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(3) VALUE 3.
       01 G-OUT.
          05 GO-T PIC X(2) OCCURS 1 TO 5 TIMES DEPENDING ON CNT.
       01 G-IN.
          05 GI-N PIC 9(3) VALUE 2.
          05 GI-T PIC X(2) OCCURS 1 TO 5 TIMES DEPENDING ON GI-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ZZ" TO GO-T (1).
           MOVE "ZZ" TO GO-T (2).
           MOVE "ZZ" TO GO-T (3).
           MOVE 5 TO GI-N.
           MOVE "YY" TO GI-T (1).
           MOVE "YY" TO GI-T (2).
           MOVE "YY" TO GI-T (3).
           MOVE "YY" TO GI-T (4).
           MOVE "YY" TO GI-T (5).
           MOVE 2 TO GI-N.
           INITIALIZE G-OUT.
           DISPLAY "A=[" GO-T (1) "][" GO-T (2) "][" GO-T (3) "]".
           INITIALIZE G-IN.
           DISPLAY "N=[" GI-N "]".
           MOVE 5 TO GI-N.
           DISPLAY "B=[" GI-T (1) "][" GI-T (4) "][" GI-T (5) "]".
           STOP RUN.
