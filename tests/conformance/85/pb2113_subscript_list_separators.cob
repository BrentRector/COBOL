      *> kb/Work PB2113 (owner ruling D10; DESIGN-frontend-grammar
      *> 9.4/9.5 D10.1) - the subscript list and the reference modifier
      *> are PARSED, not captured: ISO 8.3.5 1) "The COBOL character
      *> space is a separator" and 2) "The COBOL characters comma and
      *> semicolon, immediately followed by a space, are separators that
      *> may be used anywhere the separator space is used", so the three
      *> spellings of CELL (2, 3) below are one reference and read 23.
      *> 8.4.2.3.2 writes "qualified-data-name-1 [ ( subscript ... ) ]",
      *> outermost first (SR3); CELL holds V1 * 10 + V2 in every
      *> element. 8.4.2.3.4 GR1 c) - an index-name modified by +/-
      *> integer-1 (RX + 1, CX + 3 = 3, 4 -> 34); a data-name +/-
      *> integer the same. 8.4.3.3.2 - S (3:4) CDEF, S (V1:V2) BCD, S
      *> (8:) HIJ (the length omitted runs to the end), CELL (1 2) (2:1)
      *> the 2nd char of "12". A list may span a line end (the separator
      *> space). In an argument list 8.3.3.3.2 2) puts a literal's sign
      *> at its leftmost character and 8.7.1 surrounds an operator with
      *> spaces: MAX (V1 -1) is two arguments (2), MAX (V1 - 1) one (1).
      *> Each leg fails if the separator, a qualifier or the sign is
      *> read any other way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2113A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 ROW OCCURS 3 INDEXED BY RX.
             10 CELL PIC 99 OCCURS 4 INDEXED BY CX.
       01 K.
          05 V1 PIC 9.
          05 V2 PIC 9.
       01 S PIC X(10) VALUE "ABCDEFGHIJ".
       01 N PIC 99.
       PROCEDURE DIVISION.
       P0.
           PERFORM P1 VARYING V1 FROM 1 BY 1 UNTIL V1 > 3
               AFTER V2 FROM 1 BY 1 UNTIL V2 > 4.
           MOVE 2 TO V1.
           MOVE 3 TO V2.
           DISPLAY "SPACE  " CELL (V1 V2).
           DISPLAY "COMMA  " CELL (V1, V2).
           DISPLAY "SEMI   " CELL (V1; V2).
           DISPLAY "LIT    " CELL (3 4) " " CELL (1, 1).
           DISPLAY "QUAL   " CELL (V1 OF K V2 IN K).
           DISPLAY "REL    " CELL (V1 + 1 V2 - 1).
           SET RX TO 2.
           SET CX TO 1.
           DISPLAY "INDEX  " CELL (RX + 1, CX + 3).
           DISPLAY "MULTI  " CELL (V1
               V2).
           DISPLAY "REFMOD " S (3:4) " " S (V1:V2) " " S (8:).
           DISPLAY "BOTH   " CELL (1 2) (2:1).
           MOVE FUNCTION MAX (CELL (1 1) CELL (3 4) 7) TO N.
           DISPLAY "ARGS   " N.
           MOVE FUNCTION MAX (V1 -1) TO N.
           DISPLAY "SIGNED " N.
           MOVE FUNCTION MAX (V1 - 1) TO N.
           DISPLAY "MINUS  " N.
           STOP RUN.
       P1.
           COMPUTE CELL (V1 V2) = V1 * 10 + V2.
