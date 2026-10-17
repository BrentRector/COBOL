      *>--- conformance-case
      *> id: pb1616-omitted-branch-hex-odd
      *> rules: [SR-8.3.3.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1616 - the false path of an >>IF is part of the
      *>   initial source text, and ISO 7.2.1 requires its compiler
      *>   directives to be "syntactically correct in the initial
      *>   source text and library text". A literal that breaks its
      *>   own 8.3.3 rule is refused in an omitted branch as in a
      *>   compiled one: the screen is of the PARSED fragment, never
      *>   of the evaluated one.
      *>   cite.py --check 7.2.1 "shall be syntactically correct in
      *>     the initial source text and library text" -> OK
      *> ISO 8.3.3.2.3 SR6: X"414" has three digits where two map to
      *>   one character (COBOLNET1635).
      *>   cite.py --check 8.3.3.2.3 "Each hex-character-sequence-1
      *>     shall consist of the number of hexadecimal" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1616NOM.
       PROCEDURE DIVISION.
       MAIN-P.
       >>IF 1 = 2
       >>DEFINE D AS X"414"
       >>END-IF
           STOP RUN.
