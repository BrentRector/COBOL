*>--- conformance-case
*> id: pb492_national_edited_editing
*> rules: [SR-13.18.40.3-9, GR-13.18.40.4-2, GR-13.18.40.4-10,
*>    GR-13.18.40.5-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB492 - GR10's CHARACTER-1 LEG of category national-edited (ISO 1989:2023 8.5.2.11,
      *> 13.18.40.4 GR10, 13.18.40.3 SR9; the PICTURE EDITING phrase is COBOL-2023).
      *>
      *> GR10 names TWO ways to make a national picture national-edited: "at least one instance of
      *> character-1 OR one of the symbols from the set 'B', '0', '/'". The B/0// leg is
      *> conformance:{2002,2014,2023}/pb492_national_edited; this fixture is the character-1 leg, which
      *> the compiler used to refuse outright as an invalid PICTURE.
      *>
      *> 13.18.40.3 SR9 decides the literal's CLASS: "If USAGE IS NATIONAL is specified for the subject
      *> of the entry or if character-string-1 contains the symbol 'N', literal-1, literal-2, and
      *> literal-3 shall be national literals." Character-string-1 here contains 'N', so literal-1 is
      *> written N":" / N"-" and NOT ":" - the alphanumeric spelling is
      *> conformance:cases/13.18.40-PICTURE/pb492-national-edited-literal-class (COBOLNET1955).
      *>
      *> 13.18.40.3 SR12 makes the IS form "a fixed editing sign control symbol", and 13.18.40.5 rule 3
      *> lists it among the SIMPLE INSERTION editing symbols - "the symbols 'B', '0', '/', ',' and, if
      *> literal=1 is specified, character-1" - which is the only editing Table 7 gives this category.
      *> Rule 3 then places it: "the insertion character occupying the same character position in the
      *> edited item as the associated symbol occupies in character-string-1". 13.18.40.4 GR14's 'es'
      *> entry sizes it: "Character-1 in character-string-1 represents a character position into which
      *> the associated literal-1, literal-2, or literal-3 is to be placed. If character-1 is a simple
      *> insertion symbol or a fixed insertion symbol, the size of literal-1 is counted in the size of
      *> the item" - one character here, so each character-1 occurrence is ONE position.
      *>
      *>   NET   NNTNN      EDITING T IS N":" <- N"ABCD" => A B [:] C D       = "AB:CD"  (5)
      *>   NETM  NTNBN      EDITING T IS N"-" <- N"ABC"  => A [-] B [space] C = "A-B C"  (5)
      *>   NET2  N(2)TN(2)  EDITING T IS N"/" <- N"ABCD" => A B [/] C D       = "AB/CD"  (5)
      *> NETM is the mixed leg: GR10's two ways of being national-edited stand in ONE picture, and rule
      *> 3 treats character-1 and 'B' identically - each occupies its own position and inserts its own
      *> character.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB492NED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NET      PIC NNTNN EDITING T IS N":".
       01 NETM     PIC NTNBN EDITING T IS N"-".
       01 NET2     PIC N(2)TN(2) EDITING T IS N"/".
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"ABCD" TO NET
           MOVE N"ABC" TO NETM
           MOVE N"ABCD" TO NET2
           DISPLAY "NET=[" NET "]"
           DISPLAY "NETM=[" NETM "]"
           DISPLAY "NET2=[" NET2 "]"
      *> Class national (8.5.2.1 Table 2): the edited image compares against a national literal.
           IF NET = N"AB:CD" THEN DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO"
           STOP RUN.
