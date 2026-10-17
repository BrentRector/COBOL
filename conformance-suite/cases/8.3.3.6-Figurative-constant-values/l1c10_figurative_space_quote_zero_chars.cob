      *>--- conformance-case
      *> id: l1c10_figurative_space_quote_zero_chars
      *> rules: [GR-8.3.3.6.4-5, GR-8.3.3.6.4-8, DOC-A.1-74]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.6.4 GR5/GR8/GR1 — SPACE QUOTE ZERO char values
      *>
      *> Rows pinned: GR-8.3.3.6.4-5, GR-8.3.3.6.4-8 (first
      *> sentence), DOC-A.1-74 (alphanumeric half, --std 85).
      *>  GR5 "The space format represents one or more of the
      *>    character space in the computer's runtime coded
      *>    character set."
      *>  GR8 "The quote format represents one or more of the
      *>    quotation mark character ' " ' in the computer's
      *>    runtime coded character set."
      *>  GR1 "the character value representation of the figurative
      *>    constant ZERO (ZEROS, ZEROES), SPACE (SPACES), and QUOTE
      *>    (QUOTES) is the value of the character '0', space, and
      *>    '"', respectively ... The implementor shall specify the
      *>    unique representation". docs/CONFORMANCE.md DOC-A.1-74:
      *>    SPACE U+0020, ZERO U+0030, QUOTE U+0022; with no PROGRAM
      *>    COLLATING SEQUENCE, FUNCTION ORD gives 33 / 49 / 35.
      *> cite.py --check:
      *>  OK  §8.3.3.6.4 5)  (General rules)
      *>  OK  §8.3.3.6.4 8)  (General rules)
      *>  OK  §8.3.3.6.4 1)  (General rules)
      *>  OK  §8.3.3.6.4 2)  (General rules)   fill fixed-length item
      *>  OK  §8.3.3.6.4 3)  (General rules)   b) one char in DISPLAY
      *>  OK  §8.3.3.5.3 3)  (Syntax rules)    "" in a literal is one "
      *>  OK  §15.70.4 1)  (Returned value rules)  ORD position
      *>
      *> Derivation of each expected line:
      *>  SP=[   ]   MOVE SPACE to X(3): GR2 repeats the one-char
      *>             string to 3 positions; GR5 makes it space.
      *>  SP1=[ ]    DISPLAY SPACE: GR3 b) one char; GR5 space.
      *>  SPEQ=Y     X3 = "   " (literal of three spaces).
      *>  QT=["""]   MOVE QUOTE to X(3): GR2 + GR8, three '"'.
      *>  QT1=["]    DISPLAY QUOTE: GR3 b) one '"'.
      *>  QTEQ=Y     X3 = """""""": 8.3.3.5.3 SR3 makes each "" one
      *>             '"', so the literal content is three '"'.
      *>  ZR=[000]   MOVE ZERO to X(3): GR1/GR4 the character '0'.
      *>  ZREQ=Y     X3 = "000".
      *>  ORD=033 033 049 035   ORD of X(1) holding the literal " ",
      *>             then SPACE, ZERO, QUOTE: the DOC-A.1-74 values
      *>             33/49/35; the first two equal proves SPACE is the
      *>             same character as the literal space.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3 PIC X(3).
       01 X1 PIC X.
       01 O1 PIC 999.
       01 O2 PIC 999.
       01 O3 PIC 999.
       01 O4 PIC 999.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE SPACE TO X3.
           DISPLAY "SP=[" X3 "]".
           DISPLAY "SP1=[" SPACE "]".
           IF X3 = "   " DISPLAY "SPEQ=Y" ELSE DISPLAY "SPEQ=N".
           MOVE QUOTE TO X3.
           DISPLAY "QT=[" X3 "]".
           DISPLAY "QT1=[" QUOTE "]".
           IF X3 = """""""" DISPLAY "QTEQ=Y" ELSE DISPLAY "QTEQ=N".
           MOVE ZERO TO X3.
           DISPLAY "ZR=[" X3 "]".
           IF X3 = "000" DISPLAY "ZREQ=Y" ELSE DISPLAY "ZREQ=N".
           MOVE " " TO X1.
           MOVE FUNCTION ORD (X1) TO O1.
           MOVE SPACE TO X1.
           MOVE FUNCTION ORD (X1) TO O2.
           MOVE ZERO TO X1.
           MOVE FUNCTION ORD (X1) TO O3.
           MOVE QUOTE TO X1.
           MOVE FUNCTION ORD (X1) TO O4.
           DISPLAY "ORD=" O1 " " O2 " " O3 " " O4.
           STOP RUN.
