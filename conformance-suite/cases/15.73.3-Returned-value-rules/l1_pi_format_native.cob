      *>--- conformance-case
      *> id: l1_pi_format_native
      *> rules: [RV-15.73.3-1, FMT-15.73.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.73.2 general format and §15.73.3 r1 — FUNCTION PI
      *> The format is "<u>FUNCTION</u> <u>PI</u>": two required words
      *> and NO argument list at all - the one intrinsic shape in this
      *> family with no parentheses, which is why the complement
      *> (writing an argument list) is the negative fixture
      *> l1-pi-argument-list and not a line here.
      *> The format is written below in FOUR different statement
      *> positions - an arithmetic-expression operand inside another
      *> function's argument (COMPUTE), the SUBJECT and the OBJECT of
      *> a relation condition, the argument of another function, and a
      *> MOVE sender - because a format that binds in one channel and
      *> not another is the defect shape this corpus has hit repeatedly
      *> (pb45 found FUNCTION SQRT(x) binding as a value OBJECT under
      *> EVALUATE TRUE), so the OBJECT position has to be written and
      *> not only the subject. ISO 8.8.4.2.2 Format 1 admits
      *> literal-1 as the relation's left operand and
      *> arithmetic-expression-2 as its right, so "4 > FUNCTION PI"
      *> puts the function-identifier in the object position. It is
      *> NOT an abbreviated combined relation condition: 8.8.4.12.1
      *> abbreviates only by "the omission of the subject" or "the
      *> omission of the subject and relational operator", and this
      *> second relation states BOTH, so by 8.8.4.12.4 GR1 it is "a
      *> complete simple condition" that terminates any insertion.
      *>
      *> §15.73.3 r1: "If native arithmetic is in effect, the returned
      *> value is an implementor-defined approximation of the
      *> arithmetic expression (3 + 0.1415926535897932384626433832795)"
      *> NATIVE ARITHMETIC IS THE PRECONDITION AND IT IS ESTABLISHED,
      *> NOT ASSUMED: ISO 8.8.1.3 - "Native arithmetic is in effect
      *> when the ARITHMETIC IS NATIVE clause is specified in the
      *> OPTIONS paragraph or no ARITHMETIC clause is specified" - and
      *> this program writes no OPTIONS paragraph. (r2 and r3, the
      *> standard-binary and standard-decimal arms, are separate rows:
      *> r3 is pinned by exp_standard_decimal_eae and r2 by the
      *> declined-mode witnesses standard-binary-pi-2002/-2014.)
      *>
      *> WHAT r1 FIXES AND WHAT IT LEAVES OPEN. The VALUE approximated
      *> is fixed by the rule - it writes the arithmetic expression
      *> out in full - while the ACCURACY and the representation are
      *> left to the implementor by 15.4.1 ("When native arithmetic is
      *> in effect, the characteristics and representation of the
      *> returned value are defined by the implementor"). So the
      *> golden asserts the rule's FIXED half and reads the open half
      *> from the implementor's own documentation: docs/CONFORMANCE.md
      *> A.1 item 92 records a binary64 float carrier for a
      *> float-valued function result, whose agreement with the
      *> expression is ~1e-16, comfortably inside the 1e-9 the
      *> PIAPPROX receiver resolves. PIAPPROX subtracts the rule's own
      *> expression from the returned value and shows that the
      *> difference vanishes at scale 9; PIMOVE shows the same
      *> agreement as digits (3.14159 - the expression truncated at
      *> the receiver's 5 fraction digits, ISO 14.7.4 no-ROUNDED).
      *> Both branches of every test are written, so an implementation
      *> whose PI is wrong prints the OTHER token rather than nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PIFMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P-D PIC 9V9(9) VALUE 0.
       01 P-I PIC S9(4) VALUE 0.
       01 P-E PIC 9.9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE P-D =
               FUNCTION ABS(FUNCTION PI - (3 + 0.14159265358979324))
           IF P-D = 0
               DISPLAY "PIAPPROX=WITHIN-1E-9"
           ELSE
               DISPLAY "PIAPPROX=OUTSIDE-1E-9"
           END-IF
           IF FUNCTION PI > 3 AND 4 > FUNCTION PI
               DISPLAY "PIRANGE=3TO4"
           ELSE
               DISPLAY "PIRANGE=OTHER"
           END-IF
           COMPUTE P-I = FUNCTION INTEGER-PART(FUNCTION PI)
           IF P-I = 3
               DISPLAY "PIINT=3"
           ELSE
               DISPLAY "PIINT=OTHER"
           END-IF
           MOVE FUNCTION PI TO P-E
           DISPLAY "PIMOVE=" P-E
           STOP RUN.
