*>--- conformance-case
*> id: pb401_range_invalid_by_class
*> rules: [GR-14.7.8-1, GR-14.7.8-2, GR-14.9.13.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> EC-RANGE-INVALID IS KEYED ON A THROUGH RANGE'S CLASS, NOT ON ITS OPERANDS' WRITTEN FORM.
      *> ISO 14.7.8 THROUGH phrase opens "This specification applies to THROUGH phrases specified in the VALUE
      *> clause and the EVALUATE statement" and then splits in two: rule 1 - "When the range of values is defined
      *> by numeric literals, the range of values includes literal-1, literal-2, and all algebraic values between
      *> literal-1 and literal-2" (algebraic, no sequence, NO exception); rule 2 - "When the range of values is
      *> defined by alphanumeric or national literals, the range of values depends on the collating sequence used
      *> for evaluation of the range", which owns the sequence AND the exception: "When the value of literal-1 is
      *> greater than the value of literal-2 in the collating sequence in effect at runtime, the EC-RANGE-INVALID
      *> exception condition is set to exist, and, upon completion of any exception processing, execution proceeds
      *> as if the range of values were empty."  14.9.13.4 GR2 delegates EVALUATE's THROUGH phrase to that clause.
      *> Rule 2's "literals" is NOT an operand-form restriction: 14.7.8 introduces the phrase as "a range of
      *> values, literal-1 through literal-2" while EVALUATE's range-expression (14.9.13.2) has no literal-1 or
      *> literal-2 at all, and 14.9.13.3 SR3 - a rule about this very phrase - says "the literals or identifiers
      *> specified in the THROUGH phrase are of class alphabetic, alphanumeric, or national".
      *> Witness for kb/Work PB401. >>TURN and FUNCTION EXCEPTION-STATUS are COBOL-2002 (ISO 7.3.25, 15.22), which
      *> is why this case is filed at 2002; the range forms themselves are EVALUATE's own since 1985.
      *> ORDER IS LOAD-BEARING: FUNCTION EXCEPTION-STATUS is STICKY, so every arm that must set NOTHING is written
      *> BEFORE the first arm that sets the exception. Reversing them is how the original probe reported a false
      *> positive from the literal arm's own carry-over.
       >>TURN EC-RANGE-INVALID CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB401-RNG-CLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C   PIC X    VALUE "C".
       01 WS-M   PIC X    VALUE "M".
       01 WS-A   PIC X    VALUE "A".
       01 WS-Z   PIC X    VALUE "Z".
       01 WS-N   PIC 9(3) VALUE 005.
       01 WS-N9  PIC 9(3) VALUE 009.
       01 WS-N1  PIC 9(3) VALUE 001.
       PROCEDURE DIVISION.
       MAIN-P.
      *> Rule 1, INVERTED numeric identifier ends (9 THRU 1): empty, and NO exception condition at all.
           EVALUATE WS-N
               WHEN WS-N9 THRU WS-N1 DISPLAY "NUMINV-IN"
               WHEN OTHER            DISPLAY "NUMINV-OUT"
           END-EVALUATE.
           DISPLAY "A[" FUNCTION EXCEPTION-STATUS "]".
      *> Rule 1, ARITHMETIC-EXPRESSION ends (14.9.13.2's arithmetic-expression-3/-4; 8.8.1.1 makes an arithmetic
      *> expression's constituents numeric): 1+1 THRU 8+2 is 2 THRU 10, which contains 5 ALGEBRAICALLY.
           EVALUATE WS-N
               WHEN WS-N1 + 1 THRU 8 + 2 DISPLAY "ARITH-IN"
               WHEN OTHER                DISPLAY "ARITH-OUT"
           END-EVALUATE.
           DISPLAY "B[" FUNCTION EXCEPTION-STATUS "]".
      *> Rule 2, WELL-ORDERED identifier ends: "C" is inside "A" THRU "Z", and no exception.
           EVALUATE WS-C
               WHEN WS-A THRU WS-Z DISPLAY "OK-IN"
               WHEN OTHER          DISPLAY "OK-OUT"
           END-EVALUATE.
           DISPLAY "C[" FUNCTION EXCEPTION-STATUS "]".
      *> Rule 2, INVERTED IDENTIFIER ends - the arm the defect was about. "M" collates after "A", so the range is
      *> empty AND EC-RANGE-INVALID is set. The inversion is invisible in the source, which is exactly the case
      *> rule 2's "in the collating sequence in effect at runtime" is written for.
           EVALUATE WS-C
               WHEN WS-M THRU WS-A DISPLAY "IDINV-IN"
               WHEN OTHER          DISPLAY "IDINV-OUT"
           END-EVALUATE.
           DISPLAY "D[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
