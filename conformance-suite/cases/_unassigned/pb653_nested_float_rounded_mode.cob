*>--- conformance-case
*> id: pb653_nested_float_rounded_mode
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB653 at COBOL-2014 -- THE EIGHT-MODE ROUNDED PHRASE REACHES AN EXPRESSION WITH A NESTED
      *> FLOAT OPERAND, AND IS APPLIED ONCE, AT THE RESULTANT IDENTIFIER. The whole substance of PB653 is the
      *> 85 golden (tests/conformance/85/pb653_nested_float_operand); THIS edition adds the one thing 85 and
      *> 2002 cannot express -- the ROUNDED MODE IS phrase, a COBOL-2014 introduction (COBOLNET0803 below it;
      *> the negative is tests/conformance/negative/pb653-nested-float-rounded-mode-below-2014).
      *>
      *> It is a real behaviour difference and not a syntax copy. While a NESTED float operand was quantized
      *> at the >=9 working scale with truncation, the operand reached the store ALREADY truncated at 9, the
      *> store's own rescale was the identity, and EVERY mode therefore answered 1.732050807 -- the phrase was
      *> a no-op on this shape. Keeping the binary64 restores the discarded fraction to the ONE place entitled
      *> to round it: 14.7.4.3 rules 3-10 each speak of "the resultant identifier", and 14.7.4.1 puts the
      *> truncation there -- "If, after decimal point alignment, the number of places in the fractional part of
      *> the result of an arithmetic operation is greater than the number of places provided for the fraction
      *> of the resultant identifier, truncation is relative to the size provided for the resultant
      *> identifier". 14.7.4.3 rule 2 gives the no-phrase store its mode: "If the ROUNDED phrase is not
      *> specified, execution is as if ROUNDED MODE IS TRUNCATION had been specified".
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE EXACT DECIMAL EXPANSION OF THE BINARY64:
      *>   sqrt(3)      = 1.732050807568877193176604123436845839023590087890625
      *>       at 9 fraction digits the discarded tail is .568877193... -- ABOVE half, so every NEAREST mode
      *>       goes up, TOWARD-GREATER and AWAY-FROM-ZERO go up, TRUNCATION and TOWARD-LESSER go down.
      *>   sqrt(0.0625) = 0.25 EXACTLY (0.0625 is 2**-4, its root 2**-2) -- so at ONE fraction digit the
      *>       discarded tail is exactly half, which is the only way to tell the four NEAREST modes apart:
      *>       NEAREST-EVEN keeps 0.2 (2 is even), NEAREST-TOWARD-ZERO keeps 0.2, NEAREST-AWAY-FROM-ZERO
      *>       gives 0.3; and at NINE fraction digits 0.25 is EXACT, which is what PROHIBITED needs.
      *>   14.7.4.3 rule 7: "If the PROHIBITED phrase is specified, and the arithmetic value cannot be
      *>       represented exactly in the resultant identifier, the EC-SIZE-TRUNCATION exception condition is
      *>       set to exist, the size error condition exists" -- and the receiver is left unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB653RM14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9  PIC 9V9(9).
       01 R1  PIC 9V9.
       PROCEDURE DIVISION.
       MAIN.
      *> A -- the eight modes over ONE nested-float expression, the tail above half.
           COMPUTE R9 ROUNDED MODE IS TRUNCATION = FUNCTION SQRT(3) * 1
           DISPLAY "A1=" R9
           COMPUTE R9 ROUNDED MODE IS TOWARD-LESSER = FUNCTION SQRT(3) * 1
           DISPLAY "A2=" R9
           COMPUTE R9 ROUNDED MODE IS TOWARD-GREATER = FUNCTION SQRT(3) * 1
           DISPLAY "A3=" R9
           COMPUTE R9 ROUNDED MODE IS AWAY-FROM-ZERO = FUNCTION SQRT(3) * 1
           DISPLAY "A4=" R9
           COMPUTE R9 ROUNDED MODE IS NEAREST-EVEN = FUNCTION SQRT(3) * 1
           DISPLAY "A5=" R9
           COMPUTE R9 ROUNDED MODE IS NEAREST-TOWARD-ZERO = FUNCTION SQRT(3) * 1
           DISPLAY "A6=" R9
           COMPUTE R9 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO = FUNCTION SQRT(3) * 1
           DISPLAY "A7=" R9
      *> B -- the exact-half tail, which is the only shape that separates the NEAREST modes.
           COMPUTE R1 ROUNDED MODE IS NEAREST-EVEN = FUNCTION SQRT(0.0625) * 1
           DISPLAY "B1=" R1
           COMPUTE R1 ROUNDED MODE IS NEAREST-TOWARD-ZERO = FUNCTION SQRT(0.0625) * 1
           DISPLAY "B2=" R1
           COMPUTE R1 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO = FUNCTION SQRT(0.0625) * 1
           DISPLAY "B3=" R1
           COMPUTE R1 ROUNDED MODE IS TRUNCATION = FUNCTION SQRT(0.0625) * 1
           DISPLAY "B4=" R1
      *> C -- PROHIBITED sees the WHOLE value now, not a pre-truncated one: inexact at the receiver raises
      *> and leaves the receiver unchanged; exactly representable stores.
           MOVE 0 TO R9
           COMPUTE R9 ROUNDED MODE IS PROHIBITED = FUNCTION SQRT(3) * 1
               ON SIZE ERROR DISPLAY "C1=SIZE-ERROR"
           END-COMPUTE
           DISPLAY "C2=" R9
           COMPUTE R9 ROUNDED MODE IS PROHIBITED = FUNCTION SQRT(0.0625) * 1
               ON SIZE ERROR DISPLAY "C3=SIZE-ERROR"
           END-COMPUTE
           DISPLAY "C4=" R9
           STOP RUN.
