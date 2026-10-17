      *>--- conformance-case
      *> id: l1c01_move_edited_truncation
      *> rules: [GR-14.6.8.2-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.8.2 r5 — numeric-edited receiver: alignment, zero
      *> fill AND truncation at both ends, then editing
      *> THE RULE: "If the receiving operand is a fixed-point
      *> numeric-edited item, further alignment, zero fill or
      *> truncation, and transfer of digits take place as described in
      *> the general rules and editing rules in 13.18.40, PICTURE
      *> clause."
      *>   cite.py --check 14.6.8.2 "If the receiving operand is a
      *>     fixed-point numeric-edited item, further alignment, zero
      *>     fill or truncation, and transfer of digits take place"
      *>     -> OK §14.6.8.2 5)
      *>   cite.py --check 13.18.40.5 "Simple insertion editing results
      *>     in the insertion character occupying the same character
      *>     position in the edited item as the associated symbol"
      *>     -> OK §13.18.40.5 3)
      *>   cite.py --check 13.18.40.5 "Special insertion editing results
      *>     in the period character occupying the same character
      *>     position in the edited item as the symbol"
      *>     -> OK §13.18.40.5 4)
      *>   cite.py --check 13.18.40.5 "The second floating symbol
      *>     represents the leftmost limit of the numeric data that may
      *>     be stored in the item" -> OK §13.18.40.5 6)
      *>   cite.py --check 13.18.40.5 "If truncation occurs, the value
      *>     of the data that is used for editing is the value after
      *>     truncation as specified in 14.6.8" -> OK §13.18.40.5 6)
      *>   cite.py --check 13.18.40.5 "a single occurrence of the
      *>     replacement character(s) is (are) placed into the character
      *>     position(s) immediately preceding whichever of the
      *>     following is encountered first" -> OK §13.18.40.5 6)
      *>   cite.py --check 13.18.40.5 "the corresponding replacement
      *>     character is placed into any character position immediately
      *>     preceding whichever of the following is encountered first"
      *>     -> OK §13.18.40.5 7)
      *> Alignment is on the mask's '.'; digits beyond the mask's digit
      *> positions are cut at EITHER end, missing ones are zero filled.
      *> DERIVATION of every .out line:
      *>   A 999.99    <- 12345.678: int 345 (12 cut, high end),
      *>               frac 67 (8 cut, low end)          -> A [345.67]
      *>   B ZZ9.9     <- 1.99: frac 9 (low 9 cut); 001, Z Z
      *>               suppress the two zeros            -> B [  1.9]
      *>   C 9999.99   <- 3.5: zero fill at both ends    -> C [0003.50]
      *>   D ZZZ9.99   <- 3.5: 0003.50, Z Z Z suppress   -> D [   3.50]
      *>   E $$$9      <- 12345: digit positions are the 2nd $, 3rd $
      *>               and 9 = 3 digits: 345 (12 cut); first nonzero
      *>               at the 2nd $, so $ in position 1  -> E [$345]
      *>   F Z,ZZ9.99  <- 1234.567: 1234 fills all 4 int positions,
      *>               frac 56 (7 cut); nothing suppressed
      *>                                                 -> F [1,234.56]
      *>   G $$,$$9.99 <- 12.3: digits 0012.30; first nonzero at the
      *>               3rd $ (pos 5), so $ at pos 4; pos 1-3, with the
      *>               embedded ',', are spaces        -> G [   $12.30]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EA PIC 999.99.
       01 EB PIC ZZ9.9.
       01 EC PIC 9999.99.
       01 ED PIC ZZZ9.99.
       01 EE PIC $$$9.
       01 EF PIC Z,ZZ9.99.
       01 EG PIC $$,$$9.99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 12345.678 TO EA
           DISPLAY "A [" EA "]"
           MOVE 1.99 TO EB
           DISPLAY "B [" EB "]"
           MOVE 3.5 TO EC
           DISPLAY "C [" EC "]"
           MOVE 3.5 TO ED
           DISPLAY "D [" ED "]"
           MOVE 12345 TO EE
           DISPLAY "E [" EE "]"
           MOVE 1234.567 TO EF
           DISPLAY "F [" EF "]"
           MOVE 12.3 TO EG
           DISPLAY "G [" EG "]"
           STOP RUN.
