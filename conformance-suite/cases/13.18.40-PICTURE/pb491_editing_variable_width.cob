*>--- conformance-case
*> id: pb491_editing_variable_width
*> rules: [SR-13.18.40.3-10, SR-13.18.40.3-11, GR-13.18.40.4-12,
*>    GR-13.18.40.4-14, GR-13.18.40.5-5, GR-13.18.40.5-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB491 / PB568 - THE PICTURE EDITING PHRASE AS THE STANDARD WRITES AND RENDERS IT.
      *> Every expected image below is Annex D.24's own, or GR14's arithmetic; none is measured.
      *>
      *> SPELLING (PB568): the printed general format (13.18.40.2 Format 1, re-rendered from the canonical
      *>   PDF at printed page 441) is `EDITING character-1 { IS literal-1 | FOR { NEGATIVE IS literal-2 |
      *>   POSITIVE IS literal-3 } }` - character-1 is written BARE, with no quotation marks, exactly as
      *>   character-string-1 is, while literal-1/-2/-3 are named as literals. 13.18.40.3 SR8 types it as
      *>   "any basic letter in the COBOL character set"; 13.18.40.4 GR14 ("Character-1 in character-string-1
      *>   represents a character position") and 13.18.40.5 rule 3 make it a PICTURE SYMBOL.
      *>
      *> SIZE, 13.18.40.4 GR14 'es': "If character-1 is a simple insertion symbol or a fixed insertion symbol,
      *>   the size of literal-1 is counted in the size of the item. ... For extended editing sign control
      *>   symbols with fixed insertion, each occurrence of the character(s) specified in the associated literal
      *>   are counted in the size of the item. For floating inserting, one occurrence of literal-2 or
      *>   literal-3 is counted in the size of the item plus one character for each repetition of character-1."
      *>
      *> RENDER, 13.18.40.5 rule 5 (fixed insertion, Table 8) and rule 6 (floating insertion, Table 9). Rule 6's
      *>   first sentence is what makes a REPEATED character-1 a floating string: "The currency symbol, the
      *>   extended editing sign control symbols, if specified, and the fixed editing sign control symbols '+'
      *>   and '-' are used as the floating insertion symbols."
      *>
      *> SR12c supplies the unspecified side: "If only NEGATIVE is specified, the default character for the
      *>   unspecified phrase is the space character repeated the number of characters in literal-3."
      *>
      *> A  L999.99 with literal-2 "DEBIT " - D.24: "The statement 'MOVE -123.45 TO item' would result in
      *>    'DEBIT 123.45'" and "The statement 'MOVE 123.45 TO item' would result in 'bbbbbb123.45'". Size =
      *>    6 (literal-2, fixed insertion) + 6 ("999.99") = 12.
      *> B  LLLL9.99F with "(" and ")" - D.24: MOVE -123.45 gives 'b(123.45)'. Size = 1 + 3 (floating L:
      *>    one occurrence of literal-2 plus one character per repetition) + 4 ("9.99") + 1 (fixed F) = 9.
      *> C  LLLL9.99 with "DEBIT " - D.24 states the size outright for this shape: "would result in an item
      *>    size of 13 characters: 6 for the first 'L', 3 for the next three, and 4 for the numbers".
      *>    The floating literal lands immediately preceding the first nonzero numeric character (rule 6 a),
      *>    every position before it a space: one space, then 'DEBIT ', then '123.45'.
      *> D  XXTXX EDITING T IS "::" - the IS form is SIMPLE insertion (rule 3) and GR14 counts literal-1's
      *>    size at every occurrence: a 6-character alphanumeric-edited item rendering 'AB::CD'.
      *> E  NNTNN EDITING T IS N"::" - the national twin (GR10 admits character-1; SR9 requires the literals
      *>    to be national when character-string-1 contains 'N'). Size 2 + 2 + 2 = 6.
      *> F  999L EDITING L FOR NEGATIVE IS "CR" - fixed insertion at a TRAILING character-1, literal wider
      *>    than one: size 3 + 2 = 5; SR12c gives the positive side two spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB491VW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-ITEM PIC IS L999.99 EDITING L FOR NEGATIVE IS "DEBIT ".
       01 B-ITEM PIC IS LLLL9.99F
          EDITING L FOR NEGATIVE IS "("
          EDITING F FOR NEGATIVE IS ")".
       01 C-ITEM PIC IS LLLL9.99 EDITING L FOR NEGATIVE IS "DEBIT ".
       01 D-ITEM PIC XXTXX EDITING T IS "::".
       01 E-ITEM PIC NNTNN EDITING T IS N"::".
       01 F-ITEM PIC 999L EDITING L FOR NEGATIVE IS "CR".
       PROCEDURE DIVISION.
           MOVE -123.45 TO A-ITEM.
           DISPLAY "A-NEG=[" A-ITEM "] SIZE=" FUNCTION LENGTH(A-ITEM).
           MOVE 123.45 TO A-ITEM.
           DISPLAY "A-POS=[" A-ITEM "]".
           MOVE -123.45 TO B-ITEM.
           DISPLAY "B-NEG=[" B-ITEM "] SIZE=" FUNCTION LENGTH(B-ITEM).
           MOVE -123.45 TO C-ITEM.
           DISPLAY "C-NEG=[" C-ITEM "] SIZE=" FUNCTION LENGTH(C-ITEM).
           MOVE "ABCD" TO D-ITEM.
           DISPLAY "D=[" D-ITEM "] SIZE=" FUNCTION LENGTH(D-ITEM).
           DISPLAY "E-SIZE=" FUNCTION LENGTH(E-ITEM).
           MOVE -12 TO F-ITEM.
           DISPLAY "F-NEG=[" F-ITEM "] SIZE=" FUNCTION LENGTH(F-ITEM).
           MOVE 12 TO F-ITEM.
           DISPLAY "F-POS=[" F-ITEM "]".
           STOP RUN.
