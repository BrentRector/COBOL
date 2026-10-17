*>--- conformance-case
*> id: pb207-bit-group-level-value
*> rules: [GR-13.18.63.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR14: "If a VALUE clause is specified at the group level, subordinate items within that
      *> group shall not be described with a JUSTIFIED or SYNCHRONIZED clause, and all data items subordinate to
      *> an alphanumeric group item shall be explicitly or implicitly described with usage DISPLAY."
      *> G carries no GROUP-USAGE clause, is not strongly typed and is not a variable-length group, so it IS an
      *> alphanumeric group item (13.18.29.4 GR3), and B1 is USAGE BIT - 13.18.60.4 GR5: "The USAGE BIT clause
      *> specifies that bits shall be used to represent a boolean data item."  Not DISPLAY, so not conforming.
      *> ⛔ WHAT THIS FIXTURE PINS IS THE ATTRIBUTION.  Until kb/Work PB207 landed, this program was rejected
      *> with COBOLNET0899 - "the 13.18.63.4 GR5 area deposit for a bit-packed group is not yet implemented" -
      *> because the PB207 staging screened on DataItem.HasBitDescendant and sat in FRONT of the SR14 arm.  The
      *> program is not a compiler gap; it is non-conforming source, and telling a programmer otherwise sends
      *> them to wait for a feature instead of to their own declaration.  The CONFORMING half of that same
      *> screen - a GROUP-USAGE BIT group, whose members ARE usage bit by 13.18.29.3 SR2 and which 13.18.63.3 SR14's
      *> alphanumeric scoping therefore does not reach - is implemented and pinned by the positive golden
      *> tests/conformance/2023/pb207_bit_group_value.cob.
      *> MEASURED AT ALL FOUR EDITIONS: COBOLNET1702 is reported at every one.  At --std 85 the boolean-data
      *> gate COBOLNET0900 is reported ALONGSIDE it, not instead of it - a syntax rule the standard has
      *> carried since 1985 does not stop applying because the item's usage is a later introduction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB207N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G VALUE "AB".
          05 B1 PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY B1
           STOP RUN.
