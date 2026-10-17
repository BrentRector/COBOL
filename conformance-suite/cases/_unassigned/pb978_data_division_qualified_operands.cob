      *>--- conformance-case
      *> id: pb978_data_division_qualified_operands
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB978 - DATA DIVISION operands resolve by qualification, never by declaration order.
      *>
      *> THE RULE. ISO 8.4.2.2.3 SR1: "For each non unique user-defined name that is explicitly referenced,
      *> uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of
      *> reference." The negative twins (tests/conformance/negative/pb978-*) pin the unqualified spellings,
      *> which name two items and are refused (COBOLNET1639). This golden pins the QUALIFIED spellings, which
      *> name exactly one - and the one they name is the SECOND declared, so a first-match resolver fails it:
      *>   OCCURS 1 TO 9 DEPENDING ON CNT OF GB  - GB's CNT (5), not GA's (2), so T is five
      *>     characters (13.18.38.4 GR8 a - data-name-1 is outside the group, so its value is
      *>     used) and MOVE "ABCDEFGHI" TO T leaves T = "ABCDE" (not "AB");
      *>   66 AL RENAMES A OF G2 THRU B OF G2    - G2's A and B ("3", "4"), not G1's -> AL = "34".
      *> The RENAMES qualifiers used to be discarded as "redundant inside the owning record", which bound G1's A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB978POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA.
          05 CNT PIC 9 VALUE 2.
       01 GB.
          05 CNT PIC 9 VALUE 5.
       01 T.
          05 E PIC X OCCURS 1 TO 9 DEPENDING ON CNT OF GB.
       01 R.
          05 G1.
             10 A PIC X VALUE "1".
             10 B PIC X VALUE "2".
          05 G2.
             10 A PIC X VALUE "3".
             10 B PIC X VALUE "4".
       66 AL RENAMES A OF G2 THRU B OF G2.
       PROCEDURE DIVISION.
           MOVE "ABCDEFGHI" TO T
           DISPLAY "T=" T
           DISPLAY "AL=" AL
           STOP RUN.
