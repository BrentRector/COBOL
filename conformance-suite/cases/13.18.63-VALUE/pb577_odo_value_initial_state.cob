      *>--- conformance-case
      *> id: pb577_odo_value_initial_state
      *> rules: [GR-13.18.63.4-6, GR-13.18.63.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB577 — ISO 13.18.63.4 GR6 and GR9 at the INITIAL-STATE occasion (GR4c first bullet), which is
      *> the only occasion COBOL-85 has for them: the TO VALUE phrase of INITIALIZE is COBOL-2002 (the sibling
      *> golden conformance:2002/pb577_odo_value_initialize carries GR4c's second bullet). No spec-derived test
      *> covered either rule's ODO population, and conformance:2002/table_value_occurs covers only the FIRST of
      *> GR9's two named arms. Every expected value below is derived from the rules, not measured:
      *>   GR6 "the initialization of the associated data item behaves as if the value of the data item referenced
      *>   by the DEPENDING phrase ... is set to the maximum number of occurrences as specified by that OCCURS
      *>   clause", over its own three association cases:
      *>     G6B  case b, "an occurs-depending table" - T1 carries the VALUE; the maximum is 5, so occurrences 1,
      *>          3 and 5 all hold "AB" even though N1's VALUE is 2.
      *>     G6C  case c, "a data item that is subordinate to an occurs-depending table" - A2/B2 carry the VALUEs;
      *>          maximum 5 again, so A2(5)=CD and B2(5)=4 with N2's VALUE at 1.
      *>     G6A  case a, "a group data item that contains an occurs-depending table" - GR5 initializes the group
      *>          AREA "without consideration for the individual elementary or group items contained within this
      *>          group", and GR6 computes that area over the MAXIMUM: 5 x PIC X(2) = 10 character positions, so
      *>          the 10-character literal lands as AB CD EF GH IJ and T3(1)=AB, T3(5)=IJ.
      *>     G6O  GR6's last sentence, "If a VALUE clause is associated with the data item referenced by a
      *>          DEPENDING phrase, that value is considered to be placed in the data item AFTER the
      *>          occurs-depending table is initialized" - N4 is inside the group and holds its OWN VALUE 2 while
      *>          the table beside it carries the maximum-count seeding, which is only consistent with that order.
      *>   GR9 "A VALUE clause specified in a data description entry that CONTAINS an OCCURS clause or in an entry
      *>   that is SUBORDINATE to an OCCURS clause causes EVERY occurrence of the associated data item to be
      *>   assigned the specified value" - both named arms:
      *>     G91  arm 1, the entry contains the OCCURS clause: E9(1) = E9(3) = ABC.
      *>     G92  arm 2, the entry is subordinate to one: A9(3)=XY and B9(3)=7 - the arm table_value_occurs never
      *>          reaches, because all three of its elements carry their own OCCURS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB577OIS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9 VALUE 2.
       01 S1.
          05 T1 PIC X(2) OCCURS 1 TO 5 DEPENDING ON N1 VALUE "AB".
       01 N2 PIC 9 VALUE 1.
       01 S2.
          05 T2 OCCURS 1 TO 5 DEPENDING ON N2.
             10 A2 PIC X(2) VALUE "CD".
             10 B2 PIC 9 VALUE 4.
       01 N3 PIC 9 VALUE 2.
       01 S3 VALUE "ABCDEFGHIJ".
          05 T3 PIC X(2) OCCURS 1 TO 5 DEPENDING ON N3.
       01 S4.
          05 N4 PIC 9 VALUE 2.
          05 T4 PIC X(2) OCCURS 1 TO 5 DEPENDING ON N4 VALUE "EF".
       01 R9.
          05 E9 PIC X(3) OCCURS 3 VALUE "ABC".
          05 G9 OCCURS 3.
             10 A9 PIC X(2) VALUE "XY".
             10 B9 PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY "G6O N4=" N4.
           MOVE 5 TO N1.
           MOVE 5 TO N2.
           MOVE 5 TO N3.
           MOVE 5 TO N4.
           DISPLAY "G6B 1=[" T1(1) "] 3=[" T1(3) "] 5=[" T1(5) "]".
           DISPLAY "G6C 1=[" A2(1) B2(1) "] 5=[" A2(5) B2(5) "]".
           DISPLAY "G6A 1=[" T3(1) "] 5=[" T3(5) "]".
           DISPLAY "G6O 1=[" T4(1) "] 5=[" T4(5) "]".
           DISPLAY "G91 1=[" E9(1) "] 3=[" E9(3) "]".
           DISPLAY "G92 1=[" A9(1) B9(1) "] 3=[" A9(3) B9(3) "]".
           STOP RUN.
