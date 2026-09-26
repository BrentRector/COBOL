      *> ISO §7.2.3.4 GR11 — COPY REPLACING adds no space between
      *>   text-words that had none (pseudo-text and partial words)
      *> Rule: "The resultant text after replacement shall be in
      *>   logical free-form reference format. When copying text-words
      *>   into the resultant text, additional spaces may be introduced
      *>   only between text-words where there already exists a space
      *>   or at the end of a source line."
      *>   cite.py --check 7.2.3.4 "The resultant text after
      *>     replacement shall be in logical free-form reference
      *>     format. ..." -> OK  §7.2.3.4 11)  (General rules)
      *> Supporting rules (each run through cite.py --check):
      *>   §7.2.2.5 1) "the colon, the right parenthesis, and the left
      *>     parenthesis characters, in any context except within
      *>     alphanumeric or national literals, are treated as
      *>     separators" -> OK: in the library text X(L1G2N) the word
      *>     L1G2N is its own text-word, so ==L1G2N== matches it.
      *>   §7.2.3.4 GR9 b) LEADING / TRAILING partial-word matching and
      *>     GR9 f) "the library text-word is placed into the resultant
      *>     text with the matched characters either replaced by
      *>     partial-word-2 ..." -> OK (cite.py labels both "9) a)",
      *>     the known sub-item labelling quirk).
      *>   §8.3.5 4) "Except when appearing in a picture
      *>     character-string, the COBOL characters right parenthesis
      *>     and left parenthesis are separators." -> OK; §8.3.1 "A
      *>     character-string is delimited by separators." -> OK; §8.3.5
      *>     1) "The COBOL character space is a separator." -> OK.
      *> WHY EACH LEG CAN FAIL: the library text gl2m2sp.cpy holds no
      *>   space between "(" / ")" and L1G2N, nor inside PFX-NAME /
      *>   CNT-SFX. GR11 therefore forbids a space there in the
      *>   resultant text. A space inserted around the replacement "3"
      *>   would end the picture character-string at "X(" (a space is a
      *>   separator, 8.3.5 1)), which is not a valid picture; a space
      *>   inserted at a partial-word boundary would split L1G2P-NAME or
      *>   CNT-L1G2S into two words, so the references below would name
      *>   nothing. Either way the program would not compile.
      *> Resultant text GR11 + GR9 require (copybook after REPLACING):
      *>   01 L1G2-A PIC X(3).
      *>   01 L1G2-B PIC 9(3)V9(3).
      *>   01 L1G2P-NAME PIC X(4) VALUE "WXYZ".
      *>   01 CNT-L1G2S PIC 9(2) VALUE 42.
      *> DERIVATION of every .out line:
      *>   A=[ABC] LEN=03  "ABCDEFG" moved to X(3) keeps the leftmost 3
      *>                   characters; FUNCTION LENGTH of X(3) is 3,
      *>                   moved to PIC 99 -> 03.
      *>   B=123456 LEN=06 123.456 moved to 9(3)V9(3) is stored as the
      *>                   six digits 123456 (implied point, no
      *>                   character for it); six character positions
      *>                   -> 06.
      *>   P=WXYZ          L1G2P-NAME exists only because LEADING
      *>                   ==PFX-== BY ==L1G2P-== joined L1G2P- to NAME
      *>                   with no space; its VALUE is WXYZ.
      *>   S=42            CNT-L1G2S exists only because TRAILING ==SFX==
      *>                   BY ==L1G2S== joined CNT- to L1G2S; VALUE 42.
      *> 2023 dir: nothing in GR11 varies by edition in the 2023 text,
      *>   so the golden sits at the default edition (the LEADING /
      *>   TRAILING legs are kept out of the 85 directory: their
      *>   introducing edition is not derivable from specs/).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY gl2m2sp REPLACING ==L1G2N== BY ==3==
                              LEADING ==PFX-== BY ==L1G2P-==
                              TRAILING ==SFX== BY ==L1G2S==.
       01 W-LEN PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCDEFG" TO L1G2-A
           MOVE FUNCTION LENGTH(L1G2-A) TO W-LEN
           DISPLAY "A=[" L1G2-A "] LEN=" W-LEN
           MOVE 123.456 TO L1G2-B
           MOVE FUNCTION LENGTH(L1G2-B) TO W-LEN
           DISPLAY "B=" L1G2-B " LEN=" W-LEN
           DISPLAY "P=" L1G2P-NAME
           DISPLAY "S=" CNT-L1G2S
           STOP RUN.
