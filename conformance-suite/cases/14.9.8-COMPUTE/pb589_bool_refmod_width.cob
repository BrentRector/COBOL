*>--- conformance-case
*> id: pb589_bool_refmod_width
*> rules: [GR-14.9.8.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB589 - a boolean operand whose LENGTH is a run-time
      *> quantity. ISO 14.9.8.4 GR3: "The number of boolean positions in
      *> the value resulting from the evaluation of boolean-expression-1
      *> is the number of boolean positions in the largest boolean item
      *> referenced in the expression." A reference-modified operand is
      *> the unique data item of the SLICE (8.4.3.3.4 GR5), and a
      *> function-identifier references a temporary data item (8.4.3.2.4
      *> GR1) - BOOLEAN-OF-INTEGER's is argument-2 positions (15.13).
      *> The literal B"1111111" is not an item, so it never widens the
      *> value; the receiver R8 then zero-fills on the right (14.6.8.6).
      *> L1 - literal length (the control): slice 3 -> 111 -> 11100000.
      *> L2 - B8(1:N), N = 3: the same slice, length a data-name. Before
      *>      the fix the width was B8's FULL 8 -> 11111110.
      *> L3 - B8(K:), K = 6: the to-the-end slice is positions 6-8 = 3.
      *> L4 - B8(1:M) B-OR B2, M = 1: the largest item is B2 (2), not
      *>      the slice (1) and not B8 (8) -> 11000000.
      *> L5 - BOOLEAN-OF-INTEGER(3, W), W = 2 -> B"11" (2 positions); the
      *>      literal operand is 7 wide, the item is 2 -> 11000000 (before
      *>      the fix a run-time argument-2 counted as NO item: 11000010).
      *> L6 - the literal-argument control of L5 -> 11000000.
      *> S1 - 8.8.4.3.3 SR1 ("shall reference only boolean items of
      *>      length 1") cannot be decided at compile time for BX(1:M) or
      *>      BX(2:M); M = 1 makes both conforming, so they compile and
      *>      evaluate to BX's positions 1 (1) and 2 (0). Before the fix
      *>      they were REJECTED as 8-position operands (COBOLNET1511).
      *> E1 - 14.9.13.3 SR6 a): the same slice facing TRUE is a
      *>      one-boolean-character condition -> WHEN taken.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB589RMW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 PIC 1(8) USAGE BIT VALUE B"00000000".
       01 B2 PIC 1(2) USAGE BIT VALUE B"00".
       01 BX PIC 1(8) USAGE BIT VALUE B"10000000".
       01 R8 PIC 1(8) USAGE BIT.
       01 N  PIC 9 VALUE 3.
       01 K  PIC 9 VALUE 6.
       01 M  PIC 9 VALUE 1.
       01 W  PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R8 = B8(1:3) B-OR B"1111111"
           DISPLAY "L1=[" R8 "]"
           COMPUTE R8 = B8(1:N) B-OR B"1111111"
           DISPLAY "L2=[" R8 "]"
           COMPUTE R8 = B8(K:) B-OR B"1111111"
           DISPLAY "L3=[" R8 "]"
           COMPUTE R8 = B8(1:M) B-OR B2 B-OR B"1111111"
           DISPLAY "L4=[" R8 "]"
           COMPUTE R8 = FUNCTION BOOLEAN-OF-INTEGER(3, W)
                        B-OR B"0000001"
           DISPLAY "L5=[" R8 "]"
           COMPUTE R8 = FUNCTION BOOLEAN-OF-INTEGER(3, 2)
                        B-OR B"0000001"
           DISPLAY "L6=[" R8 "]"
           IF BX(1:M)
              DISPLAY "S1A=TRUE"
           ELSE
              DISPLAY "S1A=FALSE"
           END-IF
           IF BX(2:M)
              DISPLAY "S1B=TRUE"
           ELSE
              DISPLAY "S1B=FALSE"
           END-IF
           EVALUATE TRUE
              WHEN BX(1:M)
                 DISPLAY "E1=WHEN"
              WHEN OTHER
                 DISPLAY "E1=OTHER"
           END-EVALUATE
           STOP RUN.
