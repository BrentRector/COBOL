*>--- conformance-case
*> id: pb156_float_literal_exact_native
*> rules: [SR-8.3.3.3.3-3, GR-8.3.3.3.3-5, GR-14.9.2.4-4,
*>    GR-14.9.44.4-4, DOC-A.1-82]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB156 + PB195 - owner decision D-B (2026-08-30): a FLOATING-POINT LITERAL is its EXACT
      *> 8.3.3.3.3 rule-5 value ("the algebraic product of the value of its significand and the quantity
      *> derived by raising ten to the power of the exponent") in EVERY position and EVERY arithmetic mode.
      *> Under NATIVE arithmetic that is what 14.9.2.4 GR4 already required: "When native arithmetic is in
      *> effect and none of the operands is described with usage binary-char, binary-short, binary-long,
      *> binary-double, float-short, float-long, or float-extended, enough places shall be carried so as not
      *> to lose any significant digits during execution."  A LITERAL is not "described with usage" anything,
      *> and 14.7.7 rule 2 proves the drafters spell a floating-point literal out as its OWN bullet when they
      *> mean to include it - GR4 does not.  14.9.44.4 GR4 is the SUBTRACT twin, one word apart ("so as not
      *> to lose significant digits").
      *>
      *> Before D-B every line below lost the 19th and 20th digits to a binary64 operand: ADD 1.0E+0 answered
      *> 12345678901234567168.  Numeric design D16 is NARROWED, not overturned: an expression touching a float
      *> ITEM or a float RECEIVER still evaluates in IEEE binary64 - the FLOAT-LONG leg at the end pins that.
      *>
      *> THE INVARIANT, STATED EXACTLY (it was over-stated as "a literal's NOTATION never changes the
      *> arithmetic, only its VALUE does" - see the LANE leg at the end, which falsifies that sentence):
      *>
      *>   The VALUE a literal contributes is its exact 8.3.3.3.3 rule-5 / 8.3.3.3.2 value, in every position
      *>   and every arithmetic mode, and no operand ever arrives rounded to binary64.
      *>
      *> Each E-form line below is paired with the plain fixed-point literal of the same value, and they agree.
      *> What notation DOES still select, under native arithmetic, is the INTERMEDIATE LANE - and that is a
      *> documented 8.8.1.3 determination, not an invariant (CONFORMANCE.md A.1 item 82).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB156FLX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC 9(20) VALUE 12345678901234567890.
       01 W2 PIC 9(20) VALUE 12345678901234567890.
       01 S1 PIC 9(20) VALUE 12345678901234567890.
       01 S2 PIC 9(20) VALUE 12345678901234567890.
       01 G1 PIC 9(20).
       01 M1 PIC 9(20) VALUE 12345678901234567890.
       01 D1 PIC 9(20) VALUE 12345678901234567890.
       01 C1 PIC 9(20).
       01 P1 PIC 9V9(9).
       01 P2 PIC 9V9(9).
       01 X1 PIC 9(4) VALUE 5.
       01 N1 PIC 9(31).
       01 N2 PIC 9(31).
       01 N3 PIC 9(31).
       01 A18 PIC 9(18) VALUE 999999999999999999.
       01 B18 PIC 9(18) VALUE 999999999999999999.
       01 FL USAGE FLOAT-LONG VALUE 12345678901234567890.
       01 FOUT PIC 9(20).
       PROCEDURE DIVISION.
      *> 14.9.2.4 GR4 - ADD format 1, and the same value written without an exponent.
           ADD 1.0E+0 TO W1
           ADD 1.0 TO W2
           DISPLAY "ADD-E =" W1
           DISPLAY "ADD-F =" W2.
      *> 14.9.44.4 GR4 - the SUBTRACT twin.
           SUBTRACT 1.0E+0 FROM S1
           SUBTRACT 1.0 FROM S2
           DISPLAY "SUB-E =" S1
           DISPLAY "SUB-F =" S2.
      *> 14.9.2.4 GR2 - the GIVING format carries the same rule.
           ADD 1.0E+0 TO 12345678901234567890 GIVING G1
           DISPLAY "GIV-E =" G1.
      *> The other two arithmetic statements and COMPUTE (14.7.7 - one family, one rule).
           MULTIPLY 1.0E+0 BY M1
           DISPLAY "MUL-E =" M1.
           DIVIDE 1.0E+0 INTO D1
           DISPLAY "DIV-E =" D1.
           COMPUTE C1 = 12345678901234567890 + 1.0E+0
           DISPLAY "CMP-E =" C1.
      *> Exponentiation: the notation of the exponent literal does not select a different development.
           COMPUTE P1 = 2 ** 0.5E+0
           COMPUTE P2 = 2 ** 0.5
           DISPLAY "POW-E =" P1
           DISPLAY "POW-F =" P2.
      *> ...and an INTEGER-VALUED exponent takes the EXACT arm whatever notation it is written in (kb/Work
      *> PB272).  Power's exact Int128 arm used to ask whether the operand was WRITTEN scale-0 and non-Dec, so
      *> 10 ** 30 was the exact power while 10 ** 30.0 (scale 1) and 10 ** 3.0E+1 (a decimal128 operand) both
      *> fell through to the 8.8.1.2 binary64 approximation - one expression, three notations, answers that
      *> parted from the 17th significant digit.  The arm is keyed on the literal's VALUE now.
           COMPUTE N1 = 10 ** 30
           COMPUTE N2 = 10 ** 3.0E+1
           COMPUTE N3 = 10 ** 30.0
           DISPLAY "IPOW-F=" N1
           DISPLAY "IPOW-E=" N2
           DISPLAY "IPOW-D=" N3.
      *> ⛔ WHAT NOTATION STILL SELECTS - AND IT IS A DETERMINATION, NOT A DEFECT.  8.8.1.3 leaves "the method
      *> of evaluating an arithmetic statement" to the implementor; COBOL.NET's is that a floating-point
      *> literal operand puts the whole statement on the decimal128 INTERMEDIATE lane, even under native
      *> arithmetic, where a fixed-point literal keeps the ~38-digit Int128 one.  The two carry different
      *> intermediate precision, so an expression whose exact intermediate needs more than 34 significant
      *> digits rounds on the first and not on the second.  999999999999999999 squared is 36 digits, so:
           IF A18 * B18 + 0.0E+0 = A18 * B18
             DISPLAY "LANE  =EQ"
           ELSE
             DISPLAY "LANE  =NE"
           END-IF.
           IF A18 * B18 + 0.0 = A18 * B18
             DISPLAY "LANEF =EQ"
           ELSE
             DISPLAY "LANEF =NE"
           END-IF.
      *> kb/Work PB195 - the OPERAND-position range screen is the decimal128 one in every mode now, so a
      *> literal beyond binary64 is legal source: this used to be a hard COBOLNET1661 under native arithmetic
      *> while the identical program compiled under ARITHMETIC IS STANDARD-DECIMAL.
           IF 1.0E+400 > X1
             DISPLAY "REL   =BIG"
           ELSE
             DISPLAY "REL   =SMALL"
           END-IF.
      *> D16 NARROWED, NOT OVERTURNED - a float RECEIVER keeps the IEEE binary64 lane (8.8.1.3 leaves the
      *> method of evaluating an arithmetic statement to the implementor, and GR4's own condition is not met
      *> once an operand IS described with usage float-long).
           ADD 1.0E+0 TO FL
           MOVE FL TO FOUT
           DISPLAY "FLOAT =" FOUT.
           STOP RUN.
