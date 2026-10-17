*>--- conformance-case
*> id: pb306_concat_usage_national
*> rules: [AR-15.12.3-1, RV-15.18.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB306 - ISO §15.18.4 r2, THE USAGE LIMB: "If argument-1 is of class or usage national, the
      *> function will return a national value." The §15.18.1 table states the same in two rows, "Boolean usage
      *> National -> National" and "Numeric usage National -> National". §13.18.60.3 SR12 makes both shapes
      *> legal data: "An elementary data item with usage national shall be described with a picture
      *> character-string that describes a boolean, national, national-edited, numeric, or numeric-edited data
      *> item." Their CATEGORY is numeric / boolean, which is why a category-only reader labelled the result
      *> alphanumeric (the companion negative golden pb306-concat-usage-national-to-an is the MOVE guard).
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULES:
      *>   §15.18.4 r1 - "all of the characters in argument-1 followed by all of the characters in argument-2":
      *>       NN1 1234 + NN2 5678 = 12345678; NB1 1010 + NB2 0011 = 10100011.
      *>   THE DISCRIMINATOR IS THE RECEIVER. Each result moves to a PIC N receiver - national-to-national,
      *>       valid in Table 16 (§14.9.25.3 SR10) - and pads with spaces. The same MOVE to a PIC X receiver
      *>       is INVALID for a national sender and must be diagnosed; the companion negative golden
      *>       pb306-concat-usage-national-to-an pins that half. (BYTE-LENGTH cannot observe the result
      *>       directly: §15.14.3 r1 admits a literal, based entry, type-name or DATA ITEM, not a function.)
      *>   §15.12.1's table (BASECONVERT, "National -> National") over §15.12.3 r1's "usage display or national
      *>       data item" argument: 1234 in base 10 is 4D2 in base 16 (4*256 + 13*16 + 2 = 1234), a national
      *>       value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB306CUN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NN1 PIC 9(4) USAGE NATIONAL VALUE 1234.
       01 NN2 PIC 9(4) USAGE NATIONAL VALUE 5678.
       01 NB1 PIC 1(4) USAGE NATIONAL VALUE B"1010".
       01 NB2 PIC 1(4) USAGE NATIONAL VALUE B"0011".
       01 RN  PIC N(10).
       01 RB  PIC N(10).
       01 RC  PIC N(5).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT(NN1 NN2) TO RN
           DISPLAY "NUMERIC-USAGE-NATIONAL [" RN "]"
           MOVE FUNCTION CONCAT(NB1 NB2) TO RB
           DISPLAY "BOOLEAN-USAGE-NATIONAL [" RB "]"
           MOVE FUNCTION BASECONVERT(NN1 10 16) TO RC
           DISPLAY "BASECONVERT-USAGE-NATIONAL [" RC "]"
           STOP RUN.
