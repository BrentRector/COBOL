*>--- conformance-case
*> id: pb172_sole_function_operand
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB172 - THE REGRESSION FLOOR FOR THE RE-ATTEMPTED WIDENING. ISO
      *> 15.2: a function's type is the class and category of its result item, and
      *> "the function so described may be used anywhere a sending data item of
      *> that class and category may be specified"; 8.8.4.2.1 defines comparison
      *> for two class-alphanumeric operands. So a SOLE alphanumeric function is a
      *> legal relation / EVALUATE operand - which is exactly what PB155's
      *> widening of the 8.8.1.1 intrinsic screen broke (six NIST IF-suite
      *> programs), and why that widening had to be reverted.
      *> These lines are those six programs' shape distilled into one golden, so
      *> the ~2-minute wave gate catches an over-reach that previously took the
      *> full NIST leg to find. Both operand positions are exercised.
      *> The COMPOUND counterpart is illegal and is pinned by
      *> negative/pb172-relation-compound-alphanumeric-function: the boundary is
      *> SOLE-vs-COMPOUND, inside one statement kind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172SOLEFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC X(4) VALUE "ABCD".
       01 Y  PIC X(4) VALUE "abcd".
       01 NX PIC N(3) VALUE N"ABC".
       01 H  PIC X(2) VALUE "ab".
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION LOWER-CASE(X) = Y
               DISPLAY "LC=T" ELSE DISPLAY "LC=F" END-IF
           IF FUNCTION UPPER-CASE(Y) = X
               DISPLAY "UC=T" ELSE DISPLAY "UC=F" END-IF
           IF Y = FUNCTION LOWER-CASE(X)
               DISPLAY "RHS=T" ELSE DISPLAY "RHS=F" END-IF
           IF FUNCTION REVERSE(X) = "DCBA"
               DISPLAY "REV=T" ELSE DISPLAY "REV=F" END-IF
           IF FUNCTION LOWER-CASE(NX) = N"abc"
               DISPLAY "NAT=T" ELSE DISPLAY "NAT=F" END-IF
           EVALUATE FUNCTION LOWER-CASE(X)
               WHEN "abcd" DISPLAY "EV=HIT"
               WHEN OTHER  DISPLAY "EV=MISS"
           END-EVALUATE
           EVALUATE H
               WHEN X"6162" DISPLAY "HEX=HIT"
               WHEN OTHER   DISPLAY "HEX=MISS"
           END-EVALUATE
           STOP RUN.
