      *> kb/Work PB2639 - a FLOATING extended editing sign control symbol
      *>   (a PICTURE EDITING character-1 repeated, 13.18.40.5 rule 6) is
      *>   a DIGIT POSITION in every consumer of the mask: the render, the
      *>   ON SIZE ERROR capacity, the fraction scale and the de-editing
      *>   MOVE. Rule 6 first sentence: "The currency symbol, the extended
      *>   editing sign control symbols, if specified, and the fixed
      *>   editing sign control symbols '+' and '-' are used as the
      *>   floating insertion symbols"; fourth paragraph: "The second
      *>   floating symbol represents the leftmost limit of the numeric
      *>   data that may be stored in the item", so the first L is the
      *>   symbol itself and each later L is a digit position - to the
      *>   right of the decimal point too (rule 6 way b: all numeric
      *>   positions written with the same symbol; SR29 only requires one
      *>   insertion symbol to the LEFT of the point).
      *>   14.7.5 rule 3: a size error only when the result, "after radix
      *>   point alignment", is "further from zero than permitted".
      *>
      *>   C  LLLL9.99 EDITING L FOR NEGATIVE "DEBIT ": 3 floating digit
      *>      positions + the 9 = 4 integer digits, 2 fraction digits.
      *>      The size is 13 (Annex D.24: 6 for the first L, 3 for the
      *>      next three, 4 for the numbers). Positive side defaults to
      *>      six spaces (SR12 c).
      *>        10      -> "0010.00", L3 is the last zero: the literal
      *>                   lands there -> 1 space + 1 space + "DEBIT "
      *>                   (negative) + "10.00"
      *>        9999.99 -> fills all four integer positions, the literal
      *>                   lands on the first L (6 characters)
      *>        10000   -> five integer digits: SIZE ERROR, unchanged
      *>   G  LLL.LL EDITING L FOR NEGATIVE "-" (6 characters): integer
      *>      digits L2 and L3, fraction digits L4 and L5, positive side
      *>      one space (SR12 c). 1.5 -> "01.50": the sign lands on L2.
      *>      0.25 -> "00.25": the sign lands immediately before the
      *>      decimal point (rule 6 way a). 100 has three integer digits:
      *>      SIZE ERROR; MOVE truncates the high-order end (14.6.8.2).
      *>   De-editing MOVE (14.9.25.4 rule 5) reads the image back at the
      *>   mask scale 2: " -1.50" into 9V99 is 1.50 (absolute value,
      *>   rule 6 b), into S9V99 is -1.50.
      *>   cite.py --check 13.18.40.5 "The second floating symbol
      *>   represents the leftmost limit of the numeric data that may be
      *>   stored in the item" -> OK 13.18.40.5 6)
      *>   cite.py --check 14.7.5 "further from zero than permitted for
      *>   the associated resultant data item" -> OK 14.7.5 3)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2639FD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C-ITEM PIC IS LLLL9.99 EDITING L FOR NEGATIVE IS "DEBIT ".
       01 G-ITEM PIC IS LLL.LL EDITING L FOR NEGATIVE IS "-".
       01 N      PIC 9V99.
       01 SN     PIC S9V99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO C-ITEM.
           COMPUTE C-ITEM = -10
               ON SIZE ERROR DISPLAY "C1 size error"
               NOT ON SIZE ERROR DISPLAY "C1 [" C-ITEM "]"
           END-COMPUTE.
           COMPUTE C-ITEM = 9999.99
               ON SIZE ERROR DISPLAY "C2 size error"
               NOT ON SIZE ERROR DISPLAY "C2 [" C-ITEM "]"
           END-COMPUTE.
           COMPUTE C-ITEM = -9999.99
               ON SIZE ERROR DISPLAY "C3 size error"
               NOT ON SIZE ERROR DISPLAY "C3 [" C-ITEM "]"
           END-COMPUTE.
           COMPUTE C-ITEM = 10000
               ON SIZE ERROR DISPLAY "C4 size error"
               NOT ON SIZE ERROR DISPLAY "C4 [" C-ITEM "]"
           END-COMPUTE.
           DISPLAY "C4 kept [" C-ITEM "]".
           COMPUTE C-ITEM = -123.45
               ON SIZE ERROR DISPLAY "C5 size error"
               NOT ON SIZE ERROR DISPLAY "C5 [" C-ITEM "]"
           END-COMPUTE.
           MOVE 1.5 TO G-ITEM.
           DISPLAY "G1 [" G-ITEM "]".
           MOVE -1.5 TO G-ITEM.
           DISPLAY "G2 [" G-ITEM "]".
           MOVE G-ITEM TO N.
           DISPLAY "G2 -> 9V99 " N.
           MOVE G-ITEM TO SN.
           IF SN = -1.5 DISPLAY "G2 -> S9V99 is -1.50"
           ELSE DISPLAY "G2 -> S9V99 is wrong".
           MOVE 0.25 TO G-ITEM.
           DISPLAY "G3 [" G-ITEM "]".
           MOVE -0.25 TO G-ITEM.
           DISPLAY "G4 [" G-ITEM "]".
           MOVE 100.5 TO G-ITEM.
           DISPLAY "G5 [" G-ITEM "]".
           COMPUTE G-ITEM = 99.99
               ON SIZE ERROR DISPLAY "G6 size error"
               NOT ON SIZE ERROR DISPLAY "G6 [" G-ITEM "]"
           END-COMPUTE.
           COMPUTE G-ITEM = 100
               ON SIZE ERROR DISPLAY "G7 size error"
               NOT ON SIZE ERROR DISPLAY "G7 [" G-ITEM "]"
           END-COMPUTE.
           DISPLAY "G7 kept [" G-ITEM "]".
           STOP RUN.
