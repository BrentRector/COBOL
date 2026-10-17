      *>--- conformance-case
      *> id: pb1616-define-hex-odd
      *> rules: [SR-8.3.3.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1616 - a compiler-directive operand's literal is
      *>   lexed apart from the compilation group, so the unit's
      *>   literal screen never saw it: X"414" as a >>DEFINE value was
      *>   accepted. ISO 8.3.3.2.3 SR6: "Each hex-character-sequence-1
      *>   shall consist of the number of hexadecimal digits that the
      *>   implementor has specified as the number of hexadecimal
      *>   digits that map to an alphanumeric character." Two digits
      *>   map to one (DOC-A.1-9), so three digits are refused with the
      *>   code the same literal draws in the program text,
      *>   COBOLNET1635.
      *>   cite.py --check 8.3.3.2.3 "Each hex-character-sequence-1
      *>     shall consist of the number of hexadecimal" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1616NHX.
       PROCEDURE DIVISION.
       MAIN-P.
       >>DEFINE D AS X"414"
           STOP RUN.
