*>--- conformance-case
*> id: pb425_zero_length_sender_shapes
*> rules: [GR-8.5.4-6, GR-8.5.4-9, GR-14.9.25.4-1, GR-14.9.25.4-2, GR-14.9.25.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.25.4 GR1's zero-length-ITEM clause, asked of the two sending shapes whose
      *> LENGTH is not a stable field read (kb/Work PB425). GR1 is CONDITIONAL — "If identifier-1 IS a
      *> zero-length item, it is as if literal-1 were specified as a zero-length literal" — and §8.5.4's
      *> enumeration makes both of these shapes conditional in turn, which is what this program measures.
      *>
      *> (1) §8.5.4 item 9: "A reference-modified data item that has resolved to a length of zero, when that
      *>     has been permitted by use of the compiler directive REF-MOD-ZERO-LENGTH."  The trailing qualifier
      *>     IS the rule: outside such a region §7.3.23.3 GR1 raises EC-BOUND-REF-MOD instead of producing a
      *>     zero-length item.  Inside one, W-S(1:0) is a zero-length item, so GR1 hands it to GR2 ("literal-1
      *>     is treated as if it were the figurative constant SPACE") and the PIC 9(3) receiver takes the
      *>     space fill §8.3.3.6.4 GR2 sizes from it — the same three characters MOVE SPACE deposits, NOT the
      *>     zero an empty sender would decode to.  RM-ZL=[   ].
      *>     The SAME reference modification at a non-zero length is an ordinary alphanumeric-to-numeric
      *>     elementary move (§14.9.25.4 GR5 / Table 16): W-S(1:2) is "12", moved as an unsigned integer and
      *>     right-aligned in PIC 9(3) by §14.6.8.2.  RM-NZ=[012].
      *>
      *> (2) §8.5.4 item 6: "An intrinsic function that returns a zero-length value."  A NUMERIC returned
      *>     value never is one — §15.4 puts it in "a temporary elementary data item" whose characteristics
      *>     §15.4.1 leaves to the implementor, and a number always has at least one digit position — so a
      *>     numeric function-identifier sender is outside GR1's antecedent entirely and nothing about it is
      *>     frozen or re-described.  FUNCTION NUMVAL of "0.123456789012345" therefore reaches PIC 9V9(15)
      *>     whole: FN-NUM=[0123456789012345].
      *>     A CHARACTER-category function can be a zero-length item, and FUNCTION TRIM of an all-space
      *>     argument is one by §15.96.4 r4 ("If argument-1 contains only characters that are argument-2,
      *>     spaces if argument-2 is not specified ... the returned value is of length zero"), so it takes
      *>     GR1's route to GR2's SPACE fill exactly as the ref-mod arm does.  FN-ZL=[   ].
      *>
      *> The edition is 2023 because the REF-MOD-ZERO-LENGTH directive is a COBOL-2023 addition (§7.3.23,
      *> Annex E.3.3 item 23) and is what makes §8.5.4 item 9 reachable at all; its below-2023 rejection
      *> (COBOLNET0900) is the version matrix's own `ref-mod-zero-length-2023` construct row.  The RULE does
      *> not otherwise differ by edition, and the 2014 half of it is pb425_zero_length_item_move.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB425-ZERO-LEN-SENDER-SHAPES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S     PIC X(4)   VALUE "1234".
       01 W-BLANK PIC X(4)   VALUE "    ".
       01 W-ZERO  PIC 9      VALUE 0.
       01 W-TWO   PIC 9      VALUE 2.
       01 R-NUM   PIC 9(3).
       01 R-FRAC  PIC 9V9(15).
       PROCEDURE DIVISION.
       MAIN.
      *> ── (1) §8.5.4 item 9 — the ref-mod arm, inside the directive's region ──
       >>REF-MOD-ZERO-LENGTH ON
           MOVE 123 TO R-NUM.
           MOVE W-S(1:W-ZERO) TO R-NUM.
           DISPLAY "RM-ZL=[" R-NUM "]".
           MOVE 123 TO R-NUM.
           MOVE W-S(1:W-TWO) TO R-NUM.
           DISPLAY "RM-NZ=[" R-NUM "]".
       >>REF-MOD-ZERO-LENGTH OFF
      *> ── (2) §8.5.4 item 6 — a NUMERIC function result is never a zero-length item ──
           MOVE FUNCTION NUMVAL("0.123456789012345") TO R-FRAC.
           DISPLAY "FN-NUM=[" R-FRAC "]".
           MOVE FUNCTION TRIM(W-BLANK) TO R-NUM.
           DISPLAY "FN-ZL=[" R-NUM "]".
           STOP RUN.
