      *>--- conformance-case
      *> id: pb2732-qualified-index-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2732 - an index-name is a DEFINING user-defined
      *> word: §13.18.38.2 prints [ INDEXED BY { index-name-1 } ... ]
      *> and §8.3.2.2.11 "An index-name identifies an index associated
      *> with a specific table". A qualifier is no part of the format,
      *> so IX OF G is a syntax error. It used to compile clean and
      *> declare an index named IXOFG (the operand's glued text).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2732QI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC X OCCURS 3 INDEXED BY IX OF G.
       PROCEDURE DIVISION.
           SET IX TO 2
           DISPLAY T (IX)
           STOP RUN.
