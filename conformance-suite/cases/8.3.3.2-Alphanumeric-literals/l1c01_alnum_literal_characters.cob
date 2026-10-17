      *>--- conformance-case
      *> id: l1c01_alnum_literal_characters
      *> rules: [SR-8.3.3.2.3-2, SR-8.3.3.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.2.3 r2 and r4 — format 1 literal content: any
      *> source character; the doubled delimiter is the opener's own
      *> THE RULES:
      *>   cite.py --check 8.3.3.2.3 "Character-1 may be any character
      *>     in the coded character set that the implementor has chosen
      *>     for source code representation" -> OK §8.3.3.2.3 2)
      *>   cite.py --check 8.3.3.2.3 "The two contiguous quotation
      *>     symbols used to represent a single quotation symbol
      *>     character shall be in the same coded character set
      *>     representation as the opening quotation symbol"
      *>     -> OK §8.3.3.2.3 4)
      *>   cite.py --check 8.3.3.2.3 "Two contiguous quotation symbol
      *>     characters matching the quotation symbol used in the
      *>     opening delimiter represent a single occurrence"
      *>     -> OK §8.3.3.2.3 3)
      *>   cite.py --check 15.70.1 "The ORD function returns an integer
      *>     value that is the ordinal position of argument-1 in the
      *>     program collating sequence" -> OK §15.70.1
      *> The source and compile-time alphanumeric set is UTF-16, one
      *> character per code unit; a native ordinal is the code unit + 1
      *> (docs/CONFORMANCE.md DOC-A.1-8). This file is UTF-8 source.
      *> DERIVATION of every .out line (r2):
      *>   "é中x" is three characters (U+00E9, U+4E2D, x) -> LEN-1=3
      *>   ORD(char 1) = 0xE9 + 1 = 234          -> ORD-E=00234
      *>   ORD(char 2) = 0x4E2D + 1 = 20014      -> ORD-CJK=20014
      *>   MOVE to X(3) and DISPLAY round-trips them       -> [é中x]
      *> (r3/r4): only the opener's own symbol, in its own
      *> representation, doubles. U+FF02 FULLWIDTH QUOTATION MARK is
      *> not that representation, so "A＂＂B" holds FOUR ordinary
      *> characters; an apostrophe pair inside a quote-delimited
      *> literal is not the opener's symbol either.
      *>   "A＂＂B": 4 chars                                -> LEN-FW=4
      *>     ORD(char 2) = 0xFF02 + 1 = 65283    -> ORD-FW=65283
      *>   "A""B": 3 chars, char 2 = '"'                   -> LEN-DQ=3
      *>     ORD(char 2) = 0x22 + 1 = 35         -> ORD-DQ=00035
      *>   "A''B": 4 chars                                 -> LEN-AP=4
      *>   'A''B': 3 chars                                 -> LEN-SQ=3
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(5).
       01 L  PIC 9.
       01 X3 PIC X(3).
       01 X4 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH("é中x") TO L
           DISPLAY "LEN-1=" L
           MOVE "é中x" TO X3
           COMPUTE R = FUNCTION ORD(X3(1:1))
           DISPLAY "ORD-E=" R
           COMPUTE R = FUNCTION ORD(X3(2:1))
           DISPLAY "ORD-CJK=" R
           DISPLAY "[" X3 "]"
           MOVE FUNCTION LENGTH("A＂＂B") TO L
           DISPLAY "LEN-FW=" L
           MOVE "A＂＂B" TO X4
           COMPUTE R = FUNCTION ORD(X4(2:1))
           DISPLAY "ORD-FW=" R
           MOVE FUNCTION LENGTH("A""B") TO L
           DISPLAY "LEN-DQ=" L
           MOVE "A""B" TO X3
           COMPUTE R = FUNCTION ORD(X3(2:1))
           DISPLAY "ORD-DQ=" R
           MOVE FUNCTION LENGTH("A''B") TO L
           DISPLAY "LEN-AP=" L
           MOVE FUNCTION LENGTH('A''B') TO L
           DISPLAY "LEN-SQ=" L
           STOP RUN.
