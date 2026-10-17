*>--- conformance-case
*> id: pb392_corresponding_group_kinds
*> rules: [SR-14.9.2.3-6, SR-14.9.44.3-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB392. The CORRESPONDING operand rule is a SET of group KINDS, and the three verbs name
      *> DIFFERENT sets - which is why this program is legal at every line and the bit-group SUBTRACT in
      *> tests/conformance/negative/pb392-subtract-corresponding-bit-group.cob is not.
      *>
      *> ISO 1989:2023 14.9.44.3 SR6 (and 14.9.2.3 SR6, word for word): "Identifier-4 and identifier-5 shall be
      *> alphanumeric group items, national group items, variable-length groups, or strongly-typed group items
      *> and shall not be described with level-number 66."  A NATIONAL group is in that set - 13.18.29.4 GR2
      *> gives it usage national - so ADD and SUBTRACT CORRESPONDING admit it.
      *>
      *> ISO 14.9.25.3 SR12 says only "Identifier-3 and identifier-4 shall specify group data items", and NOTE 5
      *> under 14.9.25.4 GR11 settles the two kinds the arithmetic verbs exclude: "For purposes of MOVE
      *> CORRESPONDING, bit group items and national group items are processed as group items, rather than as
      *> elementary items."  So MOVE CORRESPONDING over two BIT groups is legal and copies the namesake pair.
      *>
      *> EXPECTED VALUES, COMPUTED FROM THE RULES (never measured):
      *>   ALNUM - 14.9.44.4 GR5 makes the statement SUBTRACT P OF A1 FROM P OF A2, i.e. 3 - 10 = -7; the
      *>           receiver is unsigned PIC 9(3), so the unsigned-receiver rule of 14.9.25.4 GR6 stores the absolute value 007.
      *>   NATL  - the same rule over the national group's namesake Q: 2 - 7 = -5, stored as 005.
      *>   BIT   - 14.9.25.4 GR11 moves B1 OF BS to B1 OF BT, so BT's B1 holds BS's 1010.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB392CORRKINDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1.
          05 P PIC 9(3) VALUE 10.
       01 A2.
          05 P PIC 9(3) VALUE 3.
       01 N1 GROUP-USAGE NATIONAL.
          05 Q PIC 9(3) VALUE 7.
       01 N2 GROUP-USAGE NATIONAL.
          05 Q PIC 9(3) VALUE 2.
       01 BS GROUP-USAGE BIT.
          05 B1 PIC 1(4) VALUE B"1010".
       01 BT GROUP-USAGE BIT.
          05 B1 PIC 1(4) VALUE B"0000".
       PROCEDURE DIVISION.
           SUBTRACT CORRESPONDING A1 FROM A2
           DISPLAY "ALNUM P=" P OF A2
           SUBTRACT CORRESPONDING N1 FROM N2
           DISPLAY "NATL  Q=" Q OF N2
           MOVE CORRESPONDING BS TO BT
           DISPLAY "BIT   B=" B1 OF BT
           STOP RUN.
