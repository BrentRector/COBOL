*>--- conformance-case
*> id: l1_divide_remainder_backmultiply_size_error
*> rules: [GR-14.9.12.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.12.4 GR7 — "When native arithmetic is in effect, the remainder is the result of
      *> multiplying the subsidiary quotient and the divisor and subtracting the product from the
      *> dividend."
      *> (cite.py --check 14.9.12.4 "the remainder is the result of multiplying the subsidiary quotient and
      *> the divisor and subtracting the product from the dividend" -> OK, §14.9.12.4 General rules 7.)
      *>
      *> THE PRODUCT GR7 NAMES IS AN INTERMEDIATE, AND §14.7.5 GOVERNS IT. Case 5 of the SIZE ERROR
      *> phrase's list raises the size error condition "if native arithmetic is in effect and the
      *> implementor defines that the range of values allowed for the intermediate data item is to be
      *> checked" — this implementation checks exactly when an ON SIZE ERROR phrase makes the check
      *> observable. So a back-multiply that overflows the intermediate shall RAISE, never wrap.
      *>
      *> DERIVED BEFORE MEASURING. The operands satisfy §14.7.7 rule 2's composite of 12 + 14 = 26 digits,
      *> so the statement is legal and its receivers are in range — yet the product GR7 forms is
      *> (subsidiary quotient) × (divisor) ≈ 10**12 × 1, carried unscaled at 14 + 14 = 28 fraction digits,
      *> i.e. of the order 10**40, which no 128-bit integer intermediate can hold. The derived behaviour
      *> is therefore: the size error condition is raised, imperative-statement-1 runs, and identifier-4
      *> keeps its previous content — 7, printed with the receiver's own 9(12)V9(9) picture as
      *> 000000000007000000000 (a V-scaled item's display carries no decimal point).
      *>   OVF=SIZEERR BR=000000000007000000000
      *> The failure this discriminates is a silent wrap: an unchecked C# Int128 multiply produces a
      *> wrong remainder with no size error, takes the NOT ON SIZE ERROR arm, and prints OVF=NONE.
      *>
      *> EDITION. The 26-digit receiver needs the 2014 arithmetic range; the version matrix owns the gate
      *> below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DIVOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BD PIC 9(12)V9(9) VALUE 999999999999.999999999.
       01 BV PIC 9V9(14)    VALUE 1.00000000000001.
       01 BQ PIC 9(12)V9(14).
       01 BR PIC 9(12)V9(9) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE BV INTO BD GIVING BQ REMAINDER BR
               ON SIZE ERROR DISPLAY "OVF=SIZEERR BR=" BR
               NOT ON SIZE ERROR DISPLAY "OVF=NONE BQ=" BQ " BR=" BR
           END-DIVIDE
           STOP RUN.
