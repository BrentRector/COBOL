      *>--- conformance-case
      *> id: pb400_evaluate_signed_literal_operand
      *> rules: [GR-14.9.13.4-1, GR-14.9.13.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.13.4 GR1 — "If an operand of the EVALUATE statement consists of a
      *> single literal, that operand is treated as a literal, not as an expression" —
      *> read with §8.3.3.3.2 rule 2 — "A literal shall not contain more than one sign
      *> character.  If a sign is used, it shall appear as the leftmost character of the
      *> literal."  A literal is one contiguous character-string, so `-5` CONSISTS OF a
      *> single literal while the SEPARATED `- 5` is a unary operator applied to one and
      *> is an arithmetic expression.  The sign is the only difference between the two,
      *> and it changes the Table-15 column (kb/Work PB400: the signed form was
      *> classified as an expression on both sides).
      *>
      *> What that literal then MEANS against an alphanumeric operand is §8.8.4.2.5:
      *> "The integer operand is treated as though it were moved, according to the rules
      *> of the MOVE statement, to an elementary data item of the same length in terms
      *> of character positions as the number of digits in the integer, and of the same
      *> class and usage as the alphanumeric or national operand" — and §14.9.25.4 GR6a
      *> governs that move: "If the sending operand is described as being signed
      *> numeric, the operational sign is not moved."  So `-5` participates as the ONE
      *> character "5", never as the two characters "-5"; §8.8.4.2.7 then space-extends
      *> the shorter operand.  The two MOVE lines at the end are the same rule stated
      *> directly, and they must agree with each other: a signed LITERAL and a signed
      *> ITEM of the same value move identically.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB400SLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A1 PIC X(1) VALUE "5".
       01 W-A2 PIC X(2) VALUE "-5".
       01 W-N  PIC S9   VALUE -5.
       01 W-R  PIC X(2) VALUE "-3".
       01 W-X2 PIC X(2) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-P.
      *> identifier × literal = 'Y'.  -5 moves de-signed into ONE character position:
      *> "5" against "5" — equal length, equal characters.
           EVALUATE W-A1
               WHEN -5
                   DISPLAY "A1-EQ"
               WHEN OTHER
                   DISPLAY "A1-NE"
           END-EVALUATE.
      *> The same literal against the two characters "-5": "5" extends to "5 " and the
      *> first pair of unequal characters is '-' against '5' — NOT equal.  The written
      *> form of the literal is NOT what participates.
           EVALUATE W-A2
               WHEN -5
                   DISPLAY "A2-EQ"
               WHEN OTHER
                   DISPLAY "A2-NE"
           END-EVALUATE.
      *> Against a NUMERIC operand §8.8.4.2.4 compares algebraic values, sign included.
           EVALUATE W-N
               WHEN -5
                   DISPLAY "N-EQ"
               WHEN OTHER
                   DISPLAY "N-NE"
           END-EVALUATE.
      *> The SEPARATED spelling is an arithmetic-expression object (Table 15 identifier
      *> × arithmetic-expression = 'Y') whose value is the same -5.
           EVALUATE W-N
               WHEN - 5
                   DISPLAY "N-EXPR-EQ"
               WHEN OTHER
                   DISPLAY "N-EXPR-NE"
           END-EVALUATE.
      *> A range's bounds are literals under the same rule (§14.9.13.4 GR4 a) 5. gives
      *> subject >= left-part AND subject <= right-part).  Both bounds de-sign to "5"
      *> and "1"; "-3" is below "5 ", so the range does not contain it.
           EVALUATE W-R
               WHEN -5 THRU -1
                   DISPLAY "R-IN"
               WHEN OTHER
                   DISPLAY "R-OUT"
           END-EVALUATE.
      *> §14.9.25.4 GR6a stated directly, for a literal and for an item of equal value.
           MOVE -5 TO W-X2.
           DISPLAY "MOVELIT[" W-X2 "]".
           MOVE W-N TO W-X2.
           DISPLAY "MOVEITM[" W-X2 "]".
           STOP RUN.
