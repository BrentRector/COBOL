      *>--- conformance-case
      *> id: pb243_display_all_literal_kinds
      *> rules: [GR-14.9.11.4-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.11.4 GR3, DISPLAY of ALL literal-1 over the literal
      *> kinds COBOL 2002 introduced: "only a single occurrence of the
      *> figurative constant is displayed. A figurative constant other
      *> than ALL national literal shall be the alphanumeric
      *> representation of that figurative constant."
      *>   cite.py --check 14.9.11.4 "only a single occurrence of the
      *>   figurative constant is displayed"  ->  OK  §14.9.11.4 3)
      *> Literal-1 "shall be an alphanumeric, boolean, or national
      *> literal, any of which may be a concatenation expression" -> OK
      *> §8.3.3.6.3 2). With no length from context, "The length of the
      *> string is the length of literal-1" -> OK §8.3.3.6.4 3), so ONE
      *> occurrence is the literal once. kb/Work PB243 (the
      *> BoundAllLiteral arm of OperandText.AsString); the COBOL-85
      *> formats are 85/pb243_display_all_literal.
      *> line 1  ALL X"4142": the hexadecimal alphanumeric literal is
      *>        "AB"; displayed once -> AB.
      *> line 2  ALL N"XY" and ALL NX"0041": the national carve-out
      *>        of GR3's second sentence; once each -> XY, A.
      *> line 3  ALL B"101" and ALL BX"A" (boolean 1010): a boolean
      *>        figurative takes its alphanumeric representation, the
      *>        characters '0'/'1' (docs/CONFORMANCE.md DOC-A.1-56: "a
      *>        boolean prints its '0'/'1' characters"); once each.
      *> line 4  the CONTRAST: the same national and boolean literals
      *>        associated with a 4- and a 6-position item ARE repeated
      *>        (§8.3.3.6.4 GR2: "the string of characters is repeated
      *>        character by character until the size of the
      *>        resultant string is greater than or equal to the
      *>        number of character positions" -> OK §8.3.3.6.4 2)).
      *> line 5  ALL over a concatenation expression: §8.8.3.2 SR1
      *>        bars an ALL figurative as a concatenation OPERAND
      *>        ("Neither literal-1 nor literal-2 shall be a figurative
      *>        constant that begins with the word ALL" -> OK §8.8.3.2
      *>        1)), so ALL "AB" & "C" is ALL ("AB" & "C") = ALL "ABC"
      *>        (§8.8.3.3 GR2) -> ABC once; the reading that binds ALL
      *>        to "AB" alone would print AC ("When a figurative
      *>        constant is specified in a concatenation expression,
      *>        the length of the string is one character" -> OK
      *>        §8.3.3.6.4 3) a)) and fail. Beside it, the same
      *>        figurative associated with 7 positions -> ABCABCA.
      *> Below 2002 each literal kind is its own introduction diagnostic
      *> (negative/pb1545-hex-alphanumeric-literal-below-2002,
      *> negative/pb71-all-national-figurative-below-2002,
      *> negative/r03-hex-national-literal-at-85,
      *> negative/pb71-all-boolean-figurative-below-2002,
      *> negative/pb1627-all-literal-concat-below-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB243A02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4 PIC N(4) VALUE ALL N"XY".
       01 B6 PIC 1(6) VALUE ALL B"101".
       01 F7 PIC X(7) VALUE ALL "AB" & "C".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" ALL X"4142" "]"
           DISPLAY "2[" ALL N"XY" "][" ALL NX"0041" "]"
           DISPLAY "3[" ALL B"101" "][" ALL BX"A" "]"
           DISPLAY "4[" N4 "][" B6 "]"
           DISPLAY "5[" ALL "AB" & "C" "][" F7 "]"
           STOP RUN.
