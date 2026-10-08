      *> PB2616 - a subscript that is an arithmetic EXPRESSION is "the result of the evaluation of
      *> arithmetic-expression-1" (8.4.2.3.4 GR1 b), not a 64-bit machine product: operands of up to 21 digits make
      *> A * B - C * D past 18 digits, and the value must select its own occurrence. Exact values (derived by hand):
      *> A = B = 10**20 + 1, C = 10**20, D = 10**20 + 2 give A * B - C * D = 1, so EL(1) = 1, + 1 selects EL(2) = 2
      *> and + 2 selects EL(3) = 3 (a wrapping 64-bit product selected EL(0) for the first). P = Q = 2**32 give
      *> P * Q = 2**64, which a 64-bit product wraps to 0, so P * Q + 2 wraps to 2 and would select EL(2) = 2 where
      *> the exact value 2**64 + 2 is no occurrence of a 3-element table: EC-BOUND-SUBSCRIPT (8.4.2.3.4 GR2), handled
      *> by a USE declarative and RESUME, the receiver unchanged. The reference-modification bounds are positions
      *> too (8.4.3.3.3 SR4): the same exact product selects the character, and an exact value past the item raises
      *> EC-BOUND-REF-MOD. A wide operand against a literal (A - 10**20 + 1 = 2) and a product of two literals past 18
      *> digits (3037000500**2 - 9223372037000249998 = 2) take the same exact lane.
       >>TURN EC-BOUND-SUBSCRIPT EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2616A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 9.
       01 TBL.
          05 EL PIC 9 OCCURS 3 TIMES.
       01 TXT PIC X(3) VALUE "XYZ".
       01 RCH PIC X VALUE "?".
       01 A PIC 9(21) VALUE 100000000000000000001.
       01 B PIC 9(21) VALUE 100000000000000000001.
       01 C PIC 9(21) VALUE 100000000000000000000.
       01 D PIC 9(21) VALUE 100000000000000000002.
       01 P PIC 9(10) VALUE 4294967296.
       01 Q PIC 9(10) VALUE 4294967296.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT
               EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 1 TO EL(1)
           MOVE 2 TO EL(2)
           MOVE 3 TO EL(3)
           MOVE 9 TO I
           MOVE EL(A * B - C * D) TO I
           DISPLAY "A*B-C*D       I=" I
           MOVE 9 TO I
           MOVE EL(A * B - C * D + 1) TO I
           DISPLAY "A*B-C*D+1     I=" I
           MOVE 9 TO I
           MOVE EL(A * B - (C * D - 2)) TO I
           DISPLAY "A*B-(C*D-2)   I=" I
           MOVE 9 TO I
           MOVE EL(P * Q + 2) TO I
           DISPLAY "P*Q+2         I=" I " (unchanged)"
           MOVE 9 TO I
           MOVE EL(P * Q) TO I
           DISPLAY "P*Q           I=" I " (unchanged)"
           MOVE 9 TO I
           MOVE EL(P + 1 - 4294967295) TO I
           DISPLAY "P+1-4294967295 I=" I
           MOVE "?" TO RCH
           MOVE TXT(A * B - C * D + 1:1) TO RCH
           DISPLAY "TXT(A*B-C*D+1:1) RCH=" RCH
           MOVE "?" TO RCH
           MOVE TXT(P * Q + 2:1) TO RCH
           DISPLAY "TXT(P*Q+2:1) RCH=" RCH " (unchanged)"
           MOVE 9 TO I
           MOVE EL(A - 100000000000000000000 + 1) TO I
           DISPLAY "A-10**20+1    I=" I
           MOVE 9 TO I
           MOVE EL(3037000500 * 3037000500 - 9223372037000249998) TO I
           DISPLAY "literals      I=" I
           STOP RUN.
