       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB553SGN.
      *> kb/Work PB553 at the INTRODUCING EDITION of the slot: the Format 2 (table) VALUE clause is a
      *> COBOL-2002 addition (13.18.63.2), and its FROM/TO operands are printed `subscript-1` /
      *> `subscript-2` -- NOT `integer-n`.  That distinction is the whole rule:
      *>   5.5 1) "When the term 'integer-n' (n = 1, 2, ...) is used in a general format and associated
      *>          rules, it refers to a fixed-point integer literal that shall be unsigned and nonzero
      *>          unless otherwise specified in the associated rules."   <- does NOT reach these slots
      *>   13.18.63.3 SR19 "Subscript-1 and subscript-2 shall be integer numeric literals."
      *>   5.5 2) a) "if that operand is a literal, it shall be an integer literal, as defined in
      *>          8.3.3.3.2, Fixed-point numeric literals"
      *>   8.3.3.3.2 2) "A literal shall not contain more than one sign character.  If a sign is used, it
      *>          shall appear as the leftmost character of the literal.  If the literal is unsigned, the
      *>          literal is nonnegative."
      *> So a SIGNED integer literal is a conforming subscript here.  It used to be
      *> `error COBOL0001: unexpected '+'` at every edition that has the clause.
      *> EXPECTED, derived from the rules and never measured:
      *>   1[ABAB]   FROM (+1) TO (+3) seeds occurrences 1..3 -- 8.3.3.3.2 2) gives +1 the value 1 and +3
      *>             the value 3, and 13.18.63.4 GR13 reuses the one literal cyclically over the range.
      *>   2[XYXY]   the mixed spelling: an unsigned FROM with a signed TO is the same range as 1..2.
      *>   3[121212] the multi-dimension odometer with every subscript signed -- GR12 increments the least
      *>             inclusive subscript first, so a 2x3 table filled from (+1 +1) to (+2 +3) reads 1 2 in
      *>             each row.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-GRP.
           05  S-T PIC X(2) OCCURS 3 VALUE "AB" FROM (+1) TO (+3).
       01  U-GRP.
           05  U-T PIC X(2) OCCURS 2 VALUE "XY" FROM (1) TO (+2).
       01  M-GRP.
           05  M-ROW OCCURS 2.
               10  M-C PIC 9 OCCURS 3 VALUES ARE 1 2 FROM (+1 +1) TO (+2 +3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" S-T(1) S-T(3) "]"
           DISPLAY "2[" U-T(1) U-T(2) "]"
           DISPLAY "3[" M-C(1 1) M-C(1 2) M-C(1 3)
                        M-C(2 1) M-C(2 2) M-C(2 3) "]"
           STOP RUN.
