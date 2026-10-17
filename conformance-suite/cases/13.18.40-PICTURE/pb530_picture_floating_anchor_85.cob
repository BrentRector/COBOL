      *>--- conformance-case
      *> id: pb530_picture_floating_anchor_85
      *> rules: [SR-13.18.40.3-27, SR-13.18.40.3-29]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB530 - !! THE COMPLEMENT OF SR29 ("For floating insertion, at least one insertion symbol
      *> shall be specified to the left of the decimal point position"): the shapes the new rule must NEVER
      *> refuse, each with the image 13.18.40.5 rules 6 and 7 derive for it. A prohibition written one symbol
      *> too wide rejects legal source, which is worse than the under-rejection it replaces - so every entry
      *> here both BINDS and EDITS, and the .out is the proof. SR29 is edition-invariant (FORMAT 1, no
      *> edition-varying element), so the 85 leg is the whole rule; the negative sibling
      *> negative/pb530-picture-floating-left-of-point rejects at all four editions.
      *>
      *> A01 $$.$$ with 1.5 - rule 6 b, the string SPANS the point ("represent all of the numeric character
      *>     positions by the same insertion symbol"), so its leftmost symbol is left of the point and the
      *>     rule is satisfied. Four '$' = three digit positions (the leftmost is the float limit, rule 6),
      *>     one left of the point and two right; 1.50 is nonzero, so the result "is the same as if the
      *>     floating insertion editing were defined only to the left of the decimal point position" and the
      *>     single replacement character is placed immediately preceding the first nonzero digit => "$1.50".
      *> A02 $$.99 with 1.5 - the string is wholly left of the point; one digit position there => "$1.50".
      *> A03 $$$$ with 12 - NO decimal point position is written at all, so every symbol is left of it (the
      *>     assumed point follows the last symbol). Three digit positions, value 012: the leading zero's
      *>     position takes the space and the '$' immediately precedes the first nonzero digit => " $12".
      *> A04 $$V99 with 1.5 - the IMPLIED point, with the string to its left. 'V' is not counted in the size
      *>     (13.18.40.4 GR14), so the item is four characters: "$1" then the two fraction digits => "$150".
      *> A05 ++.++ with -1.5 - the floating-SIGN leg of A01. Table 9: the symbol '+' renders '-' over a
      *>     negative value => "-1.50".
      *> A06 $$$PP with 1500 - a TRAILING 'P' string puts the assumed decimal point to its RIGHT (13.18.40.4
      *>     GR14), so the floating string is left of it. The 'P's are not counted in the size but are counted
      *>     in the digit positions, so 1500 is held as 15 scaled by 100 in a three-character item => "$15".
      *> A07 .ZZ with 0.5 - !! ZERO SUPPRESSION IS NOT FLOATING INSERTION. SR29 names floating insertion
      *>     alone and the standard states no analogue for 'Z' or '*', so this stays legal and renders its
      *>     two fraction digits => ".50". This entry is what keeps the new rule from being widened to
      *>     13.18.40.5 rule 7 by a later reader.
      *> A08 .99 with 0.5 - no floating insertion at all: SR29 has nothing to say => ".50".
      *> A09 +$$$ with 12 - NOTE 5 of SR27 names this string VALID by name ("the picture character-string
      *>     +$$$ is valid, but ... +++$$$ is invalid"), and the invalid half is the negative sibling
      *>     negative/pb530-picture-precedence-residue. The lone '+' is FIXED insertion (13.18.40.5 rule 5,
      *>     one occurrence is not a string) and the three '$' are the ONE floating string, two digit
      *>     positions: 12 is nonzero in both, so the replacement character sits immediately left => "+$12".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530ANC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A01 PIC $$.$$.
       01 A02 PIC $$.99.
       01 A03 PIC $$$$.
       01 A04 PIC $$V99.
       01 A05 PIC ++.++.
       01 A06 PIC $$$PP.
       01 A07 PIC .ZZ.
       01 A08 PIC .99.
       01 A09 PIC +$$$.
       PROCEDURE DIVISION.
           MOVE 1.5 TO A01
           MOVE 1.5 TO A02
           MOVE 12 TO A03
           MOVE 1.5 TO A04
           MOVE -1.5 TO A05
           MOVE 1500 TO A06
           MOVE 0.5 TO A07
           MOVE 0.5 TO A08
           MOVE 12 TO A09
           DISPLAY "A01=[" A01 "]"
           DISPLAY "A02=[" A02 "]"
           DISPLAY "A03=[" A03 "]"
           DISPLAY "A04=[" A04 "]"
           DISPLAY "A05=[" A05 "]"
           DISPLAY "A06=[" A06 "]"
           DISPLAY "A07=[" A07 "]"
           DISPLAY "A08=[" A08 "]"
           DISPLAY "A09=[" A09 "]"
           STOP RUN.
