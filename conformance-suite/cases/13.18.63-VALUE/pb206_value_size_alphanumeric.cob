      *>--- conformance-case
      *> id: pb206_value_size_alphanumeric
      *> rules: [SR-13.18.63.3-4, SR-13.18.63.3-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206P1.
      *> ISO 13.18.63.3 SR4 - "If the item is of category alphabetic,
      *> alphanumeric, or alphanumeric-edited literals in the VALUE
      *> clause shall be alphanumeric literals.  Alphanumeric literals
      *> in the VALUE clause of an elementary item shall not exceed the
      *> size indicated by an explicit PICTURE clause.  Alphanumeric
      *> literals in the VALUE clause of an alphanumeric group item
      *> shall not exceed the size of the group item."
      *>
      *> THE CONFORMING HALF of kb/Work PB206.  BOTH size sentences had
      *> NO implementation anywhere in the compiler while their national
      *> (SR5) and boolean (SR10) twins had theirs, so both silently
      *> TRUNCATED: measured on 1d949007, `01 E1 PIC X(2) VALUE "ABCD".`
      *> displayed AB and `01 GZ VALUE "ABCDEF". 05 O1 PIC X(2).
      *> 05 O2 PIC X(2).` displayed ABCD, neither with a diagnostic.
      *> They are now COBOLNET1740 (negative fixtures
      *> pb206-value-oversize-elementary, pb206-group-value-oversize).
      *> A screen that also OVER-rejected would pass both of those, so
      *> THIS program pins the population the rule admits.
      *>
      *> EE / GE - the BOUNDARY.  The rule is "shall not EXCEED", so a
      *>   literal of exactly the size is conforming; an off-by-one
      *>   screen would reject these.
      *> ES - a SHORTER literal.  GR7 aligns it per 14.6.8
      *>   (left-justified, space fill), it is not a violation.
      *> GA / EA - `ALL "literal"` is a FIGURATIVE constant (8.3.3.6.2
      *>   format 6), not an alphanumeric literal, and 8.3.3.6.4 GR2 -
      *>   the branch for "the length of the string is specified in the
      *>   rules for the context", which names the VALUE clause - says
      *>   it "is repeated character by character until the size of the
      *>   resultant string is greater than or equal to the number of
      *>   character positions in the associated data item" and is then
      *>   "truncated from the right".  So a 3-character literal-1 over
      *>   4 positions is CONFORMING SOURCE seeding XYZX, and the size
      *>   screen must not measure it.  This is the arm a naive
      *>   `decoded length > size` screen breaks.
      *> GZ - the same rule for a figurative WORD: one character
      *>   (8.3.3.6.4 GR1 - the value of '0' in the runtime coded
      *>   character set) repeated to the group's four positions.
      *> 13.18.63.4 GR5 gives the group answers: the area is
      *> initialized "without consideration for the individual
      *> elementary or group items contained within this group", so the
      *> characters land positionally and each member is whatever its
      *> own description makes of the characters under it.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EE PIC X(4) VALUE "ABCD".
       01 ES PIC X(4) VALUE "AB".
       01 EA PIC X(4) VALUE ALL "PQ".
       01 GE VALUE "ABCD".
          05 GE1 PIC X(2).
          05 GE2 PIC X(2).
       01 GA VALUE ALL "XYZ".
          05 GA1 PIC X(2).
          05 GA2 PIC X(2).
       01 GZ VALUE ZEROS.
          05 GZ1 PIC X(2).
          05 GZ2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "EE=[" EE "]"
           DISPLAY "ES=[" ES "]"
           DISPLAY "EA=[" EA "]"
           DISPLAY "GE=[" GE1 "][" GE2 "]"
           DISPLAY "GA=[" GA1 "][" GA2 "]"
           DISPLAY "GZ=[" GZ1 "][" GZ2 "]"
           STOP RUN.
