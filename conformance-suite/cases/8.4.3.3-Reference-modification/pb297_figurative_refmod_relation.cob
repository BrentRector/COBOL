      *>--- conformance-case
      *> id: pb297_figurative_refmod_relation
      *> rules: [GR-8.4.3.3.4-4, GR-8.4.3.3.4-5, GR-8.8.4.2.7-1, GR-8.8.4.2.7-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB297G1.
      *> ISO 8.3.3.6.4 GR2 - "When a figurative constant represents a
      *> string of one or more characters and the length of the string
      *> is specified in the rules for the context in which the
      *> figurative constant is used, then when this figurative
      *> constant is specified in a VALUE clause or in association with
      *> a fixed-length data item, literal, or intermediate result, the
      *> string of characters is repeated character by character until
      *> the size of the resultant string is greater than or equal to
      *> the number of character positions in the associated data item,
      *> literal, or intermediate result.  This resultant string is
      *> then truncated from the right until the number of character
      *> positions remaining is equal either to 1 or to the number of
      *> character positions in the associated data item, literal, or
      *> intermediate result, whichever is greater."
      *> ISO 8.3.3.6.4 GR2 NOTE 1 - "A figurative constant is
      *> associated with a data item or literal when, for example, the
      *> figurative constant is moved to it, COMPARED WITH IT, or
      *> paired with it in a binary operation."
      *>
      *> WHICH OPERAND IS THE ASSOCIATED ONE (kb/Work PB297).  For a
      *> reference-modified operand it is THE SLICE, not the item the
      *> slice was carved from: 8.4.3.3.4 GR5 - "Reference modification
      *> creates a unique data item that is a subset of the data item
      *> referenced by identifier-1" - and GR5 c) - "The evaluation of
      *> length specifies the number of bit positions or character
      *> positions of the data item to be used in the operation."  The
      *> compiler used to size the figurative from the BASE item's
      *> PICTURE, so every T01..T09 line below answered the opposite of
      *> what these rules require.  8.8.4.2.7 rule 2 then hid it for
      *> SPACE alone: "comparison proceeds as though the shorter
      *> operand were extended on the right by sufficient alphanumeric
      *> spaces" - the pad and the figurative coincide, so T10 was
      *> right by accident while T01 was wrong.  T10 is kept as the
      *> no-regression guard for exactly that reason.
      *>
      *> Every expected value below is DERIVED, not measured.  LOW-VALUE
      *> is the lowest and HIGH-VALUE the highest ordinal position in
      *> the native runtime collating sequence (8.3.3.6.4 GR6/GR7); no
      *> PROGRAM COLLATING SEQUENCE clause is present, so the native
      *> sequence is in effect and 8.8.4.2.7's standard comparison
      *> applies.
      *>  T01 X(1:1) is one position holding LOW-VALUE; GR2 sizes the
      *>      figurative to ONE position -> equal-length, equal -> Y.
      *>  T02 widths already coincide (2 and 2) -> Y (the control that
      *>      passed even with the defect).
      *>  T03 the unmodified item, 4 and 4 -> Y (second control).
      *>  T04 a non-1 leftmost-position, one position -> Y.
      *>  T05 leftmost-position and length are DATA ITEMS (I=2, L=2),
      *>      so no compile-time width for the slice exists at all;
      *>      positions 2..3 are LOW-VALUE -> Y.
      *>  T06 length omitted: GR5 c) - "the unique data item extends
      *>      from and includes the position identified by
      *>      leftmost-position up to and including the rightmost
      *>      position" -> positions 3..4, two LOW-VALUEs -> Y.
      *>  T07 the HIGH-VALUE twin of T01 -> Y.
      *>  T08 ORDERING, not equality: the two operands are equal at
      *>      length 1, so LESS is false -> N.
      *>  T09 HIGH-VALUE slice against LOW-VALUE at length 1: the
      *>      highest ordinal position is greater than the lowest -> Y.
      *>  T10 the SPACE case the defect could not reach -> Y.
      *>  T11 LOW-VALUE slice against SPACE: lowest ordinal position vs
      *>      space, unequal -> N.
      *>  T12 ALL "ab" sized to 2 -> "ab"; A(1:2) is "ab" -> Y.
      *>  T13 ALL "ab" sized to 3: repeated to "abab" (>= 3), then
      *>      truncated from the right to 3 -> "aba"; A(1:3) is
      *>      "aba" -> Y.
      *>  T14 ALL "ab" sized to 1 -> "a"; A(1:1) is "a" -> Y.
      *>  T15 A(2:2) is "ba" against ALL "ab" sized to 2 = "ab";
      *>      position 1 differs -> N.
      *>  T16 TWO figuratives: no associated data item exists, so GR2
      *>      does not apply and 8.3.3.6.4 GR3 c) gives each "the
      *>      length of literal-1" -> "ab" and "aba"; 8.8.4.2.7 rule 2
      *>      extends the shorter with a space -> "ab " vs "aba",
      *>      differing at position 3 -> N.  (8.8.4.2.3 SR3 admits two
      *>      literal operands: "All literals shall be of class
      *>      alphanumeric, national, or numeric".)
      *>  T17 GR3 b) - "When a figurative constant is other than ALL
      *>      literal-1, the length of the string is one character" ->
      *>      one LOW-VALUE against one LOW-VALUE -> Y.
      *>  T18 GR3 b) gives SPACE one position, GR3 c) gives ALL "  "
      *>      two; rule 2 space-extends the shorter -> Y.
      *>  T19 the figurative on the LEFT of the relation -> Y.
      *>  T20 the figurative on the left, ORDERING, against a slice:
      *>      HIGH-VALUE sized to 2 exceeds two LOW-VALUEs -> Y.
      *>  T21 the negated relation over the same pair as T01 -> N.
      *>  T22 a table element's slice -> Y.
      *>  T23 EVALUATE lowers to the same relation -> Y.
      *>  T24 a reference-modified GROUP (8.4.3.3.3 SR1 admits "an
      *>      alphanumeric group item"): G(1:2) is two LOW-VALUEs -> Y.
      *>  T25 G(3:2) is two spaces -> Y.
      *>  T26 the UNMODIFIED group still sizes to the group's own four
      *>      positions: "<LOW><LOW>  " is not four LOW-VALUEs -> N.
      *>
      *> Reference modification, figurative constants and ALL literal-1
      *> are all COBOL-85 constructs and no Annex E change touches
      *> these rules, so this program is 85 source; the edition axis is
      *> pinned by FigurativeSizingTests.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC X(4) VALUE LOW-VALUES.
       01 H  PIC X(4) VALUE HIGH-VALUES.
       01 A  PIC X(4) VALUE "abab".
       01 S  PIC X(4) VALUE SPACES.
       01 G.
          05 G1 PIC X(2) VALUE LOW-VALUES.
          05 G2 PIC X(2) VALUE SPACES.
       01 T.
          05 TE PIC X(4) OCCURS 3 TIMES.
       01 I  PIC 9 VALUE 2.
       01 L  PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE LOW-VALUES TO TE(2)
           IF X(1:1) = LOW-VALUE
              DISPLAY "T01=Y" ELSE DISPLAY "T01=N" END-IF
           IF X(1:2) = LOW-VALUES
              DISPLAY "T02=Y" ELSE DISPLAY "T02=N" END-IF
           IF X = LOW-VALUES
              DISPLAY "T03=Y" ELSE DISPLAY "T03=N" END-IF
           IF X(2:1) = LOW-VALUE
              DISPLAY "T04=Y" ELSE DISPLAY "T04=N" END-IF
           IF X(I:L) = LOW-VALUES
              DISPLAY "T05=Y" ELSE DISPLAY "T05=N" END-IF
           IF X(3:) = LOW-VALUES
              DISPLAY "T06=Y" ELSE DISPLAY "T06=N" END-IF
           IF H(1:1) = HIGH-VALUE
              DISPLAY "T07=Y" ELSE DISPLAY "T07=N" END-IF
           IF H(1:1) < HIGH-VALUE
              DISPLAY "T08=Y" ELSE DISPLAY "T08=N" END-IF
           IF H(1:1) > LOW-VALUE
              DISPLAY "T09=Y" ELSE DISPLAY "T09=N" END-IF
           IF S(1:1) = SPACE
              DISPLAY "T10=Y" ELSE DISPLAY "T10=N" END-IF
           IF X(1:1) = SPACE
              DISPLAY "T11=Y" ELSE DISPLAY "T11=N" END-IF
           IF A(1:2) = ALL "ab"
              DISPLAY "T12=Y" ELSE DISPLAY "T12=N" END-IF
           IF A(1:3) = ALL "ab"
              DISPLAY "T13=Y" ELSE DISPLAY "T13=N" END-IF
           IF A(1:1) = ALL "ab"
              DISPLAY "T14=Y" ELSE DISPLAY "T14=N" END-IF
           IF A(2:2) = ALL "ab"
              DISPLAY "T15=Y" ELSE DISPLAY "T15=N" END-IF
           IF ALL "ab" = ALL "aba"
              DISPLAY "T16=Y" ELSE DISPLAY "T16=N" END-IF
           IF LOW-VALUE = LOW-VALUE
              DISPLAY "T17=Y" ELSE DISPLAY "T17=N" END-IF
           IF SPACE = ALL "  "
              DISPLAY "T18=Y" ELSE DISPLAY "T18=N" END-IF
           IF LOW-VALUE = X(1:1)
              DISPLAY "T19=Y" ELSE DISPLAY "T19=N" END-IF
           IF HIGH-VALUE > X(1:2)
              DISPLAY "T20=Y" ELSE DISPLAY "T20=N" END-IF
           IF X(1:1) NOT = LOW-VALUE
              DISPLAY "T21=Y" ELSE DISPLAY "T21=N" END-IF
           IF TE(2)(1:1) = LOW-VALUE
              DISPLAY "T22=Y" ELSE DISPLAY "T22=N" END-IF
           EVALUATE X(1:1)
             WHEN LOW-VALUE DISPLAY "T23=Y"
             WHEN OTHER     DISPLAY "T23=N"
           END-EVALUATE
           IF G(1:2) = LOW-VALUES
              DISPLAY "T24=Y" ELSE DISPLAY "T24=N" END-IF
           IF G(3:2) = SPACES
              DISPLAY "T25=Y" ELSE DISPLAY "T25=N" END-IF
           IF G = LOW-VALUES
              DISPLAY "T26=Y" ELSE DISPLAY "T26=N" END-IF
           STOP RUN.
