      *> kb/Work PB530 / PB855 - the COBOL-2023 leg: ISO 1989:2023 13.18.40.3 SR25's second sentence is a
      *> PLACEMENT rule, and these are the spellings that obey it. "When extended editing sign control symbols
      *> are used and two are specified, the first occurrence of the EDITING phrase shall be for the leftmost
      *> symbol in character-string-1 and the second occurrence shall be for the rightmost symbol in
      *> character-string-1." The negative siblings negative/pb530-picture-editing-phrase-order (the reversed
      *> spellings, COBOLNET1984) and negative/pb855-picture-editing-es-placement (a symbol where no
      *> currency symbol could stand, COBOLNET1935; two symbols that are not the string's two ends,
      *> COBOLNET1984) hold what it refuses; this golden holds the images the legal spellings render.
      *>
      *> E01 F999.99L, the 'F' phrase first, with -1.5. Both extended symbols render their NEGATIVE literal
      *>     (Table 9: "character-1 NEGATIVE phrase ... literal-2" over a negative value), each at its own
      *>     position, over 3 + 2 digit positions => "(001.50)". The reversed phrase order renders
      *>     ")001.50(" - the same characters at the opposite ends, which is why the rule exists.
      *> E02 L9999.99F with -123.45 - Annex D.24's OWN example, "it is quite common to represent negative
      *>     items by enclosing them in parentheses". Four integer digit positions hold 0123 => "(0123.45)".
      *> E03 L99F with -12 - the pair at the two ends of a string with no point => "(12)".
      *> E04 9999L with -12.3 - a SINGLE extended symbol takes the precedence of a fixed currency symbol
      *>     (13.18.40.6: "has the same precedence as the 'cs' symbol in the column and row of non-floating
      *>     insertion symbols"), here the trailing one: last. Four digit positions, no decimal point
      *>     position, so -12.3 is held as 0012 => "0012(". (The mid-string spelling 99L99 is refused:
      *>     negative/pb855-picture-editing-es-placement.)
      *> E05 L$999 with -12 - SR26's SECOND sentence, the LEGAL leg: "the currency symbol when used shall
      *>     be either the leftmost symbol in character-string-1, optionally preceded by character-1". The
      *>     'L' renders its NEGATIVE literal, the fixed '$' its own position => "($012".
      *> E06 999$L with -12 - the other leg, "the rightmost symbol ... optionally followed by character-1"
      *>     => "012$(". The ILLEGAL placement (99$9L) is negative/pb530-picture-editing-currency-placement.
      *> E07 L999L with -12 - TWO FIXED occurrences of ONE character-1 under one phrase. Rule 6's floating
      *>     insertion is "a string of at least two identical" symbols, an adjacency property and not a
      *>     count of occurrences, so these are two fixed insertion symbols (rule 5), each rendering the
      *>     NEGATIVE literal at its own position => "(012(". (The occurrence count made the compiler read
      *>     them as one floating string and print "(0012".)
      *> E08 L$999F with -12 - the currency symbol second, between the two extended symbols, which are the
      *>     string's leftmost and rightmost symbols (SR25) => "($012)".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530EOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E01 PIC F999.99L EDITING F FOR NEGATIVE IS "("
                           EDITING L FOR NEGATIVE IS ")".
       01 E02 PIC L9999.99F EDITING L FOR NEGATIVE IS "("
                            EDITING F FOR NEGATIVE IS ")".
       01 E03 PIC L99F EDITING L FOR NEGATIVE IS "("
                       EDITING F FOR NEGATIVE IS ")".
       01 E04 PIC 9999L EDITING L FOR NEGATIVE IS "(".
       01 E05 PIC L$999 EDITING L FOR NEGATIVE IS "(".
       01 E06 PIC 999$L EDITING L FOR NEGATIVE IS "(".
       01 E07 PIC L999L EDITING L FOR NEGATIVE IS "(".
       01 E08 PIC L$999F EDITING L FOR NEGATIVE IS "("
                         EDITING F FOR NEGATIVE IS ")".
       PROCEDURE DIVISION.
           MOVE -1.5 TO E01
           MOVE -123.45 TO E02
           MOVE -12 TO E03
           MOVE -12.3 TO E04
           MOVE -12 TO E05
           MOVE -12 TO E06
           MOVE -12 TO E07
           MOVE -12 TO E08
           DISPLAY "E01=[" E01 "]"
           DISPLAY "E02=[" E02 "]"
           DISPLAY "E03=[" E03 "]"
           DISPLAY "E04=[" E04 "]"
           DISPLAY "E05=[" E05 "]"
           DISPLAY "E06=[" E06 "]"
           DISPLAY "E07=[" E07 "]"
           DISPLAY "E08=[" E08 "]"
           STOP RUN.
