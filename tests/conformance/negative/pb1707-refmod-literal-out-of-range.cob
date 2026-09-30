      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1707 part 1 (owner decision R60). ISO 8.4.3.3.4 5) c): "The sum of leftmost-position and
      *> length minus the value one shall be less than or equal to the number of positions in the data item
      *> referenced by identifier-1" (cite.py OK). CX has 5 positions and CX(1:10) needs 10, so the statement
      *> is the fatal EC-BOUND-REF-MOD; with checking NOT enabled here, ISO 14.6.13.1.3 8) (last paragraph,
      *> cite.py OK) lets the compiler produce no code for a fatal condition it detects, and the positions
      *> being integer literals over an item of fixed size, it does (COBOLNET2670, as GnuCOBOL refuses
      *> "length of 'CX' out of bounds"). With checking enabled the same statement is COBOLNET2671, a
      *> warning, and compiles: conformance:2023/l1_fatal_ec_runtime_detection.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGRMLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CX PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
           DISPLAY CX(1:10).
           STOP RUN.
