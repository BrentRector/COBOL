      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1968 - ISO 8.8.1.2 4): "The ways in which
      *> identifiers, literals, operators, and parentheses may be
      *> combined in arithmetic expressions are summarized in Table 3",
      *> and Table 3 marks a unary operator followed by a unary operator
      *> an invalid pair. A subscript is arithmetic-expression-1
      *> (8.4.2.3.2), so X (- - 2) - two SEPARATED signs - is refused
      *> here exactly as COMPUTE N = - - 2 is (COBOLNET1719). It
      *> compiled clean and read X (2) while the subscript was a flat
      *> token run the formation screen never saw; since kb/Work PB2113
      *> the subscript is parsed in place and the one screen reaches it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1968N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 X PIC 9 OCCURS 3 VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY X (- - 2).
           STOP RUN.
