      *>--- conformance-case
      *> id: l1c35_unstring_receiving_operands
      *> rules: [SR-14.9.48.3-9, FMT-14.9.48.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.3 SR9 — UNSTRING INTO / DELIMITER IN are receiving
      *>   operands
      *> "The data item referenced by identifier-4 is the receiving
      *>   operand for
      *> data. The data item referenced by identifier-5 is the receiving
      *>   operand
      *> for delimiters."
      *> OK  §14.9.48.3 9)  (Syntax rules)
      *> Supporting rules (cite.py --check, all OK):
      *> OK  §14.9.48.4 11) c) "shall be moved into the current
      *>   receiving area
      *>     according to the rules for the MOVE statement"
      *> OK  §14.9.48.4 11) d) "shall be moved into the data item
      *>   referenced by
      *>     identifier-5 according to the rules for the MOVE statement"
      *> OK  §14.9.25.4 6) 3. (cite.py labels the list item "6) 3."; it
      *>   is
      *>     GR6 d) 3.) "the sending operand is treated as if it were an
      *>     unsigned integer of category numeric"
      *> OK  §14.6.8.2 4) "aligned by decimal point ... zero fill or
      *>     truncation on either end as required"
      *> OK  §14.6.8.5 "aligned at the leftmost character position ...
      *>   with
      *>     space fill or truncation to the right, as required"
      *> OK  §13.18.32.4 2) "the data is aligned at the rightmost
      *>   character
      *>     position"
      *> Derivation. S1 = "12,ABCDE::XY" (12 chars), DELIMITED BY "," OR
      *>   "::".
      *> Every receiver is preset so that a store is visible.
      *> N1=0012   the first area "12" (GR11c, alphanumeric) is MOVEd
      *>   into
      *>           the numeric receiver N1 PIC 9(4): a 2-digit unsigned
      *>           integer, decimal-aligned, zero-filled on the left.
      *> D1=[,  ]  delimiter "," MOVEd into D1 PIC X(3): space fill
      *>   right.
      *> J1=[ ABCDE] "ABCDE" MOVEd into J1 PIC X(6) JUSTIFIED RIGHT.
      *> D2=[:]    delimiter "::" MOVEd into D2 PIC X: truncated on
      *>   right.
      *> A1=[XY ]  "XY" (end of sender) MOVEd into A1 PIC X(3).
      *> D3=[   ]  the delimiting condition is the end of identifier-1,
      *>   so
      *>           identifier-5 is space filled (GR11d).
      *> S1=[12,ABCDE::XY] identifier-1 is the sending operand (SR8),
      *>   and
      *>           is not changed.
      *> A receiver not treated as a receiving operand of a MOVE would
      *>   show
      *> "12  " in N1, "ABCDE " in J1, or keep its preset value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(12) VALUE "12,ABCDE::XY".
       01 N1 PIC 9(4) VALUE 9999.
       01 J1 PIC X(6) JUSTIFIED RIGHT VALUE "******".
       01 A1 PIC X(3) VALUE "***".
       01 D1 PIC X(3) VALUE "***".
       01 D2 PIC X VALUE "*".
       01 D3 PIC X(3) VALUE "***".
       PROCEDURE DIVISION.
           UNSTRING S1 DELIMITED BY "," OR "::"
               INTO N1 DELIMITER IN D1
                    J1 DELIMITER IN D2
                    A1 DELIMITER IN D3
           END-UNSTRING
           DISPLAY "N1=" N1
           DISPLAY "D1=[" D1 "]"
           DISPLAY "J1=[" J1 "]"
           DISPLAY "D2=[" D2 "]"
           DISPLAY "A1=[" A1 "]"
           DISPLAY "D3=[" D3 "]"
           DISPLAY "S1=[" S1 "]"
           STOP RUN.
