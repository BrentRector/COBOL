      *>--- conformance-case
      *> id: pb2017_arithmetic_operator_spacing
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB2017 - the SPACING of the arithmetic operators.
      * ISO 8.7.1: the arithmetic operators are "specific COBOL
      *   characters that shall be preceded by a space and followed by
      *   a space except that no space is required between a left
      *   parenthesis and a unary operator or between a unary operator
      *   and a left parenthesis".  (cite.py: OK 8.7.1)
      * 8.3.3.3.2 2): "If a sign is used, it shall appear as the
      *   leftmost character of the literal" - so -2 below is a
      *   numeric literal, not an operator.  (cite.py: OK)
      * 8.3.5 1): a line end is a space (the lexer reads it as one),
      *   so an operator may end or begin a line.
      * Every spelling below is the conforming one, and each leg can
      * fail on its own: the program must COMPILE (the rule's
      * negatives pb2017-* are the refusals) and print the values the
      * arithmetic of 8.8.1.2 gives.
      *
      *   A=6 B=3   T = "123"   I = 1
      *   P1  A + B * 2 - 1       6+6-1        =  11
      *   P2  (A + B) / 3 ** 2    9/9          =   1
      *   P3  - A + B             (-6)+3       =  -3
      *   P4  -(A + B)            unary vs '(' =  -9
      *   P5  (- A) + B           '(' vs unary =  -3
      *   P6  A * -2              signed lit   = -12
      *   P7  A - -2              6+2          =   8
      *   P8  A * (- B)                        = -18
      *   P9  A * -(B)                         = -18
      *   P10 A +<line end> B     line end     =   9
      *   P11 T (I + 1)           subscript    =   2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2017P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 6.
       01 B PIC S9(4) VALUE 3.
       01 R PIC S9(4).
       01 E PIC -9(4).
       01 TT PIC X(3) VALUE "123".
       01 T-TABLE REDEFINES TT.
          05 T PIC 9 OCCURS 3 TIMES.
       01 I PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A + B * 2 - 1.
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = (A + B) / 3 ** 2.
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = - A + B.
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = -(A + B).
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = (- A) + B.
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = A * -2.
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = A - -2.
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = A * (- B).
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = A * -(B).
           MOVE R TO E
           DISPLAY E.
           COMPUTE R = A +
               B.
           MOVE R TO E
           DISPLAY E.
           DISPLAY T (I + 1).
           STOP RUN.
       END PROGRAM PB2017P1.
