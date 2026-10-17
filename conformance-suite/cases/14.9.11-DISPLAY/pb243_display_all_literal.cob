      *>--- conformance-case
      *> id: pb243_display_all_literal
      *> rules: [GR-14.9.11.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.11.4 GR3, DISPLAY of a figurative constant: "only a
      *> single occurrence of the figurative constant is displayed".
      *>   cite.py --check 14.9.11.4 "only a single occurrence of the
      *>   figurative constant is displayed"  ->  OK  §14.9.11.4 3)
      *> kb/Work PB243: the BARE-figurative arm of OperandText.AsString
      *> was pinned (2002/ca3_figurative_display_pcs); its ALL literal-1
      *> arm (BoundAllLiteral) was pinned by nothing. This golden pins
      *> every figurative format COBOL-85 has in a DISPLAY operand.
      *> Each line is bracketed so a missing or repeated occurrence, or
      *> a stray separator, changes the bytes.
      *> line 1  ALL "AB" -> AB. §8.3.3.6.4 GR3 c): with no length
      *>        from context "The length of the string is the length
      *>        of literal-1" -> OK §8.3.3.6.4 3); so ONE occurrence
      *>        is the literal once. A repeating implementation (the
      *>        MOVE arm) prints ABAB..., a one-character one prints A.
      *> line 2  the same over a 3- and a 1-character literal-1.
      *> line 3  ALL symbolic-character-1 (Format 7) and the bare
      *>        symbolic-character: STAR IS 43 is "the coded
      *>        character at ordinal position integer-1 in the native
      *>        alphanumeric character set" -> OK §12.3.7.4 11) b);
      *>        the native set is UTF-16, ordinal n = code point n-1
      *>        (docs/CONFORMANCE.md DOC-A.1-188), so 43 is "*", once.
      *> line 4  ALL ZEROES / ALL SPACES / ALL QUOTES: "When a
      *>        figurative constant is other than ALL literal-1, the
      *>        length of the string is one character" -> OK
      *>        §8.3.3.6.4 3) b); values per §8.3.3.6.4 GR1 ('0',
      *>        space, '"'). The space sits between brackets, so the
      *>        harness's trailing-space trim (kb/Work PB150) cannot
      *>        hide it.
      *> line 5  the CONTRAST: the same ALL "AB" associated with a
      *>        6-position item IS repeated (§8.3.3.6.4 GR2: "the
      *>        string of characters is repeated character by
      *>        character until the size of the resultant string is
      *>        greater than or equal to the number of character
      *>        positions" -> OK §8.3.3.6.4 2)), so line 1's single
      *>        occurrence is DISPLAY's rule and not the literal's.
      *> The 2002 literal kinds (X, N, NX, B, BX and the concatenation
      *> expression) are 2002/pb243_display_all_literal_kinds; each is
      *> rejected at 85 by its literal's own introduction gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB243A85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS STAR IS 43.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F PIC X(6) VALUE ALL "AB".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" ALL "AB" "]"
           DISPLAY "2[" ALL "XYZ" "][" ALL "Q" "]"
           DISPLAY "3[" ALL STAR "][" STAR "]"
           DISPLAY "4[" ALL ZEROES "][" ALL SPACES "][" ALL QUOTES "]"
           DISPLAY "5[" F "]"
           STOP RUN.
