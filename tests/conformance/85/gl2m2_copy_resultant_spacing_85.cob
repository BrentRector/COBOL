      *> ISO §7.2.3.4 GR11 - COPY REPLACING adds no space between
      *>   text-words that had none: the 85 TWIN of
      *>   2023/gl2m2_copy_resultant_spacing, pseudo-text leg only
      *>   (no LEADING, no TRAILING, no FUNCTION).
      *> Rule: "The resultant text after replacement shall be in
      *>   logical free-form reference format. When copying text-words
      *>   into the resultant text, additional spaces may be introduced
      *>   only between text-words where there already exists a space
      *>   or at the end of a source line."
      *>   cite.py --check 7.2.3.4 "The resultant text after
      *>     replacement shall be in logical free-form reference
      *>     format. ..." -> OK  §7.2.3.4 11)  (General rules)
      *> Supporting rules (each run through cite.py --check):
      *>   §7.2.2.5 1) the left and right parenthesis "are treated as
      *>     separators" during text manipulation -> OK: in the
      *>     library text X(L1G2N) the word L1G2N is its own
      *>     text-word, so ==L1G2N== matches it (§7.2.3.4 GR9).
      *>   §8.3.5 1) "The COBOL character space is a separator." -> OK;
      *>   §8.3.1 "A character-string is delimited by separators."
      *>     -> OK.
      *> WHY IT CAN FAIL: the library text gl2m2sp.cpy (a byte copy of
      *>   the 2023 one) holds no space between "(" / ")" and L1G2N.
      *>   GR11 therefore forbids a space there in the resultant text.
      *>   A space inserted next to the replacement "3" would end the
      *>   picture character-string at "X(" / "9(", which is not a
      *>   valid picture, and the program would not compile.
      *> Resultant text GR11 + GR9 require (the two items used):
      *>   01 L1G2-A PIC X(3).
      *>   01 L1G2-B PIC 9(3)V9(3).
      *>   (PFX-NAME and CNT-SFX are copied unchanged and unused.)
      *> DERIVATION of every .out line:
      *>   A=[ABC]   "ABCDEFG" moved to X(3) keeps the leftmost 3
      *>             characters.
      *>   B=123456  123.456 moved to 9(3)V9(3) is stored as the six
      *>             digits 123456 (implied point, no character).
      *> 85 dir: GR11 and pseudo-text REPLACING are COBOL-85; the
      *>   LEADING / TRAILING legs stay in the 2023 golden only, since
      *>   their introducing edition is not derivable from specs/.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M1E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY gl2m2sp REPLACING ==L1G2N== BY ==3==.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCDEFG" TO L1G2-A
           DISPLAY "A=[" L1G2-A "]"
           MOVE 123.456 TO L1G2-B
           DISPLAY "B=" L1G2-B
           STOP RUN.
