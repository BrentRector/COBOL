*>--- conformance-case
*> id: pb41_scaled_position
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.2.3.4 GR1b / 8.4.3.3.4 rule 5)c - a SUBSCRIPT and a REFERENCE-
      *> MODIFIER position are the VALUE of an arithmetic expression, never the
      *> storage of one (fix-queue PB41). GR1b: "the subscript is the result of the
      *> evaluation of arithmetic-expression-1"; 8.8.1.1 admits "an identifier
      *> referencing a numeric data item", so a PIC 9V9 item is a legal subscript.
      *>
      *> COBOL.NET stores numerics UNSCALED - PIC 9V9 VALUE 2.0 is the field 20L at
      *> scale 1 - so the VALUE (2) and the STORAGE (20) are different numbers
      *> whenever the PICTURE has a V. Reading the storage is what made W-E(W-S)
      *> with W-S = 2.0 index occurrence 20, land outside 1..5, and DISPLAY the
      *> benign out-of-range scratch: compiled clean, ran to completion, wrong
      *> answer. The ONE ordinal-position read now de-scales for both positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB41SCALED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       01 W-H.
          05 W-T PIC 9(2) OCCURS 5 TIMES INDEXED BY W-IX.
       01 W-A PIC X(9) VALUE "ABCDEFGHI".
       01 W-S PIC 9V9 VALUE 2.0.
       01 W-P PIC 9V9 VALUE 1.5.
       01 W-Q PIC 9V9 VALUE 1.5.
       01 W-R PIC 9(2).
       01 W-U PIC X(2).
       01 W-C PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 11 TO W-E (1).
           MOVE 22 TO W-E (2).
           MOVE 33 TO W-E (3).
           MOVE 11 TO W-T (1).
           MOVE 22 TO W-T (2).
      *> 1 - a SCALED data-name subscript. The value of W-S is 2, so W-E(2) = 22.
           MOVE W-E (W-S) TO W-R.
           DISPLAY "SCALED=" W-R.
      *> 2 - a COMPOUND segment whose operands are both scaled. GR1b tests the
      *> integrality of THE RESULT, not of each operand: 1.5 + 1.5 = 3.0 is an
      *> integral result and a legal subscript naming occurrence 3. De-scaling the
      *> operands FIRST would give 1 + 1 = 2 and raise the condition twice on
      *> source that never violated it, which is why a compound segment carrying a
      *> scaled operand evaluates as one expression instead of token-by-token.
           MOVE W-E (W-P + W-Q) TO W-R.
           DISPLAY "COMPOUND=" W-R.
      *> 3 - a SCALED reference-modifier leftmost-position (8.4.3.3.3 SR4 makes
      *> both ref-mod positions arithmetic expressions). Value 2, length 2 -> "BC".
           MOVE W-A (W-S:2) TO W-U.
           DISPLAY "RMSCALED=[" W-U "]".
      *> 4 - THE SIBLING ORDINAL CHANNELS, measured rather than assumed. SET FROM
      *> a scaled item (14.9.39, over an INDEXED BY index-name from the 13.18.38
      *> OCCURS clause) and a PERFORM VARYING bound compared
      *> against one both already read the VALUE, so PB41 was specific to the two
      *> positions above and did not generalize to every ordinal use of a scaled
      *> item. An OCCURS DEPENDING ON item cannot be scaled at all - 13.18.38.3
      *> SR17 "Data-name-1 shall describe an integer" makes that illegal source,
      *> which COBOLNET0852 already rejects.
           SET W-IX TO W-S.
           MOVE W-T (W-IX) TO W-R.
           DISPLAY "SETIDX=" W-R.
           PERFORM VARYING W-R FROM 1 BY 1 UNTIL W-R > W-S
               ADD 1 TO W-C
           END-PERFORM.
           DISPLAY "VARYING=" W-C.
           STOP RUN.
