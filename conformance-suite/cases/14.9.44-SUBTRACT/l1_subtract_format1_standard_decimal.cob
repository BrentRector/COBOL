      *>--- conformance-case
      *> id: l1_subtract_format1_standard_decimal
      *> rules: [GR-14.9.44.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.4 GR1, SECOND PARAGRAPH — the half of the format
      *> 1 rule that only exists under standard arithmetic: "When
      *> standard-decimal arithmetic, or standard-binary arithmetic is
      *> in effect, the result of the initial evaluation is equivalent
      *> to the result of the arithmetic expression
      *> (operand-11 + operand-12 + … + operand-1n) … The result of
      *> the subtraction from the value of each data item referenced
      *> by identifier-2 is equivalent to the result of the arithmetic
      *> expression (identifier-2 - initial-evaluation)".
      *> ARITHMETIC IS STANDARD-DECIMAL puts that mode in effect
      *> (§8.8.1.5.1); the NATIVE half of the same rule is pinned by
      *> 2023/l1_subtract_format1_initial_evaluation, whose four lines
      *> these four mirror statement for statement.  The two modes
      *> must agree here, and that agreement is the point: the rule
      *> defines the standard-arithmetic result AS an expression, so
      *> the mode may change how the intermediate is carried but not
      *> what value reaches the receivers.
      *> STANDARD-BINARY, the rule's other named mode, is a declared
      *> non-support (owner decision D13 / the PB260-PB261 family) and
      *> is deliberately not exercised here.
      *>
      *> EVERY VALUE IS EXACT IN THE MODE'S OWN ARITHMETIC, so nothing
      *> below depends on a rounding choice: §8.8.1.5.1 defines
      *> standard-decimal as ISO/IEC 60559 decimal128, and 1, 2.5,
      *> 0.4, 100.00, 200.00, 5 and 10 are all exactly representable
      *> decimals whose sums and differences are exact.
      *>
      *> SUM  — initial evaluation 1 + 2.5 = 3.5, formed once and
      *>        stored into each receiver left to right (§14.7.7 rule
      *>        4 b): A = 100.00 - 3.5 = 96.50, B = 200.00 - 3.5 =
      *>        196.50, shown through PIC 999.99.
      *> ONCE — the discriminator for "the SUM of such operands":
      *>        C = 10 - (0.4 + 0.4) = 9.2, and with no ROUNDED phrase
      *>        §14.7.4.3 GR2/GR10 truncate toward zero into PIC 9(3)
      *>        -> 009.  Subtracting the operands one at a time
      *>        through the receiver would give 008.
      *> OVR  — §14.7.7 rule 4 a) and NOTE 3: E is both the sending
      *>        operand and the first receiver, and the results are as
      *>        if no receiver shared storage with a sender, so
      *>        E = 5 - 5 = 0 and F = 10 - 5 = 5.
      *> MIN  — (identifier-2 - initial-evaluation) puts the RECEIVER
      *>        on the left: H = 4 - 10 = -6.  Only the sign separates
      *>        this from the reversed order, so H carries SIGN IS
      *>        LEADING SEPARATE (§13.18.52.4 GR6 — a leading non-digit
      *>        character position holding the basic special character
      *>        for negative) and prints -006.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBF1SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 9(3)V99 VALUE 100.00.
       01 B  PIC 9(3)V99 VALUE 200.00.
       01 EA PIC 999.99.
       01 EB PIC 999.99.
       01 C  PIC 9(3) VALUE 10.
       01 E  PIC 9(3) VALUE 5.
       01 F  PIC 9(3) VALUE 10.
       01 H  PIC S9(3) SIGN IS LEADING SEPARATE VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT 1 2.5 FROM A B
           MOVE A TO EA
           MOVE B TO EB
           DISPLAY "SUM A=" EA " B=" EB
           SUBTRACT 0.4 0.4 FROM C
           DISPLAY "ONCE C=" C
           SUBTRACT E FROM E F
           DISPLAY "OVR E=" E " F=" F
           SUBTRACT 10 FROM H
           DISPLAY "MIN H=" H
           STOP RUN.
