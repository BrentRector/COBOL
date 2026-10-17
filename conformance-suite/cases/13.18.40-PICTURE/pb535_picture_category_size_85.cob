      *>--- conformance-case
      *> id: pb535_picture_category_size_85
      *> rules: [GR-13.18.40.4-3, GR-13.18.40.4-4, GR-13.18.40.4-6,
      *>    GR-13.18.40.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB535 - ISO 1989:2023 13.18.40.4 GR4, MEASURED AS DISPLACEMENT. "The size in boolean
      *> positions or character positions of an elementary data item that has been defined with a PICTURE
      *> clause is determined by the number of symbols in character-string-1 that represent either boolean
      *> positions or character positions." GR14 names the three symbols that represent none - 'P' ("not
      *> counted in the size of the item"), 'V' ("not counted in the size of the item") and 'S' ("counted in
      *> the size of the item only when the subject of the entry is described with a SIGN clause with the
      *> SEPARATE phrase") - and says of every other symbol "is counted in the size of the item".
      *>
      *> Each group below is filled by ONE alphanumeric group MOVE and the group's LAST member is displayed,
      *> so what the golden reads is the DISPLACEMENT the preceding item causes: an item one position short
      *> of its picture shifts every following member, which is precisely the harm PB535 recorded when the
      *> alphanumeric arm counted a whitelist of the symbols it expected instead of GR4's symbols - PIC XX,XX
      *> sized 4 where GR4 gives 5, PIC XXCR sized 2 where GR4 gives 4. GR4 is a general rule of every
      *> edition from 1985 on, so 85 is the edition to pin it at.
      *>
      *> S01 AAAA is 4 (GR5 alphabetic; four 'A', each counted), so "ABCDEF" leaves "EF".
      *> S02 XXX9 is 4 (GR6 alphanumeric - at least one 'X'; 'X' and '9' both counted).
      *> S03 A9 is 2 (GR6's other leg - two DIFFERENT symbols from 'A', 'X', '9' and no 'X' at all).
      *> S04 XX0XX is 5 and S05 XX/XX is 5 (GR7 alphanumeric-edited; GR14 counts '0' and '/' as character
      *>     positions into which the zero and the slant are inserted during editing).
      *> S06 S999 is 3, NOT 4: no SIGN SEPARATE phrase is specified, so GR14 does not count the 'S'.
      *> S07 9(3)PP is 3: GR14's 'P' is "not counted in the size of the item, but each symbol 'P' is counted
      *>     in the maximum number of digit positions".
      *> S08 9V99 is 3: GR14's 'V' is not counted either.
      *> S09 999CR is 5: GR14's '+ - CR DB' entry counts EACH CHARACTER used in the symbol, so the two
      *>     characters of 'CR' are two character positions.
      *> S10 ZZ,ZZ9.99 is 9: every symbol of a numeric-edited mask is a character position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB535SZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G-S01.
          05 S01 PIC AAAA.
          05 T01 PIC XX.
       01 G-S02.
          05 S02 PIC XXX9.
          05 T02 PIC XX.
       01 G-S03.
          05 S03 PIC A9.
          05 T03 PIC XX.
       01 G-S04.
          05 S04 PIC XX0XX.
          05 T04 PIC XX.
       01 G-S05.
          05 S05 PIC XX/XX.
          05 T05 PIC XX.
       01 G-S06.
          05 S06 PIC S999.
          05 T06 PIC XX.
       01 G-S07.
          05 S07 PIC 9(3)PP.
          05 T07 PIC XX.
       01 G-S08.
          05 S08 PIC 9V99.
          05 T08 PIC XX.
       01 G-S09.
          05 S09 PIC 999CR.
          05 T09 PIC XX.
       01 G-S10.
          05 S10 PIC ZZ,ZZ9.99.
          05 T10 PIC XX.
       PROCEDURE DIVISION.
           MOVE "ABCDEFGHIJK" TO G-S01
           MOVE "ABCDEFGHIJK" TO G-S02
           MOVE "ABCDEFGHIJK" TO G-S03
           MOVE "ABCDEFGHIJK" TO G-S04
           MOVE "ABCDEFGHIJK" TO G-S05
           MOVE "ABCDEFGHIJK" TO G-S06
           MOVE "ABCDEFGHIJK" TO G-S07
           MOVE "ABCDEFGHIJK" TO G-S08
           MOVE "ABCDEFGHIJK" TO G-S09
           MOVE "ABCDEFGHIJK" TO G-S10
           DISPLAY "S01=[" G-S01 "] T01=[" T01 "]"
           DISPLAY "S02=[" G-S02 "] T02=[" T02 "]"
           DISPLAY "S03=[" G-S03 "] T03=[" T03 "]"
           DISPLAY "S04=[" G-S04 "] T04=[" T04 "]"
           DISPLAY "S05=[" G-S05 "] T05=[" T05 "]"
           DISPLAY "S06=[" G-S06 "] T06=[" T06 "]"
           DISPLAY "S07=[" G-S07 "] T07=[" T07 "]"
           DISPLAY "S08=[" G-S08 "] T08=[" T08 "]"
           DISPLAY "S09=[" G-S09 "] T09=[" T09 "]"
           DISPLAY "S10=[" G-S10 "] T10=[" T10 "]"
           STOP RUN.
