      *>--- conformance-case
      *> id: l1_subtract_format1_initial_evaluation
      *> rules: [GR-14.9.44.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.4 GR1 (SUBTRACT statement, format 1) — the
      *> initial evaluation, and what is subtracted from what.  Four
      *> output lines, one per clause of the rule; native arithmetic
      *> is in effect (§11.9.5.2 GR4 — no ARITHMETIC clause is as if
      *> NATIVE were written), and the standard-decimal half of the
      *> same rule is pinned by its sibling
      *> 2023/l1_subtract_format1_standard_decimal.
      *>
      *> LINE 1 (SUM) — "the initial evaluation consists of
      *> determining the value to be subtracted, which is literal-1 or
      *> the value of the data item referenced by identifier-1, OR IF
      *> MORE THAN ONE IS SPECIFIED, THE SUM OF SUCH OPERANDS.  The
      *> initial evaluation is subtracted from the value of the data
      *> item referenced by identifier-2".  1 + 2.5 = 3.5, formed
      *> once; §14.7.7 rule 4b stores the one intermediate into each
      *> receiver left to right.  A = 100.00 - 3.5 = 96.50 and
      *> B = 200.00 - 3.5 = 196.50, displayed through PIC 999.99
      *> (§14.6.8.2 rule 5 -> §13.18.40.5 rule 4, where '.' is the
      *> SPECIAL insertion editing symbol — simple insertion is the
      *> closed set §13.18.40.5 rule 3 names, and '.' is not in it)
      *> as 096.50 and 196.50.  Composite of operands
      *> (SR1 a): 3 integer + 2 fraction digits, far inside §14.7.7
      *> rule 2 a)'s 31.
      *>
      *> LINE 2 (ONCE) is the DISCRIMINATOR for "the SUM of such
      *> operands", which line 1 alone cannot supply: subtracting the
      *> two operands one after the other would reach the same answer
      *> there.  C is PIC 9(3) VALUE 10 and the operands are 0.4 and
      *> 0.4.  Per the rule the initial evaluation is 0.8 and C
      *> becomes 10 - 0.8 = 9.2; no ROUNDED phrase is written, so
      *> §14.7.4.3 GR2 makes it "as if ROUNDED MODE IS TRUNCATION",
      *> and GR10 truncates toward zero, giving 009.  A statement that
      *> subtracted each operand in turn THROUGH THE RECEIVER would
      *> compute 10 - 0.4 = 9.6 -> 009, then 009 - 0.4 = 8.6 -> 008.
      *>
      *> LINE 3 (OVR) is the sending/receiving OVERLAP that §14.7.7
      *> rule 4 a) and NOTE 3 fix: all item identification for the
      *> operands of the initial evaluation is done at the START of
      *> the statement, and "the results in the receiving operands are
      *> the same as if no receiving operand shared any part of its
      *> storage area with any sending operand".  E is both the sole
      *> sending operand and the FIRST receiver.  The initial
      *> evaluation is E's statement-start value 5, so E = 5 - 5 = 0
      *> and F = 10 - 5 = 5.  A compiler that re-read E after storing
      *> into it would leave F at 10.
      *>
      *> LINE 4 (MIN) pins the rule's last sentence — "The result of
      *> the subtraction from the value of each data item referenced
      *> by identifier-2 is equivalent to the result of the arithmetic
      *> expression (identifier-2 - initial-evaluation)" — i.e. the
      *> RECEIVER is the minuend, not the subtrahend.  H starts at 4
      *> and the initial evaluation is 10, so H = 4 - 10 = -6.  Only
      *> the SIGN can distinguish this from the reversed operand order
      *> (the magnitudes are equal), so H is described SIGN IS LEADING
      *> SEPARATE, where §13.18.52.4 GR6 makes the sign a leading
      *> non-digit character position carrying the basic special
      *> character for negative: the four characters -006.  Reversed
      *> operands would print +006.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBF1NA.
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
