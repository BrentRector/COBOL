*>--- conformance-case
*> id: l1_factorial_eae_branches
*> rules: [RV-15.36.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.36.4 r1 — FACTORIAL's equivalent arithmetic expression, ALL THREE lettered arms:
      *>   a) when the value of argument-1 is 0 or 1,   (1)
      *>   b) when the value of argument-1 is 2,        (2)
      *>   c) when the value of argument-1 is n,        (n * (n - 1) * (n - 2) * ... * 1)
      *>
      *> --check validated:
      *>   cite.py --check 15.36.4 "The equivalent arithmetic expression is as follows"
      *>     -> OK §15.36.4 1)
      *>   cite.py --check 15.36.1 "The type of this function is integer." -> OK §15.36.1
      *>   cite.py --check 15.36.3 "Argument-1 shall be an integer greater than or equal to zero."
      *>     -> OK §15.36.3 1) - every argument written below is a nonnegative integer, so each
      *>     reference is conforming source and r1's arms are what decide the value.
      *>   cite.py --check 15.4.1 "the returned value shall equal the value of the equivalent
      *>     arithmetic expression" -> OK §15.4.1 1)
      *>
      *> ⛔ WHY STANDARD-DECIMAL. §15.4.1 r1's "shall EQUAL" is scoped "When standard-decimal
      *> arithmetic or standard-binary arithmetic is in effect and an equivalent arithmetic
      *> expression is specified"; under NATIVE the same subclause gives only "an
      *> implementor-defined approximation of the value of that expression". Under ARITHMETIC IS
      *> STANDARD-DECIMAL every digit below is therefore REQUIRED BY THE STANDARD rather than
      *> merely produced by this implementation. The native lane of the same three arms is
      *> 85/l1_factorial_eae_native, written relationally for exactly that reason.
      *>
      *> ⛔ THE ARMS THE CORPUS DID NOT HAVE. r1a names TWO values and every existing golden used
      *> only the first - 2023/pb48_figurative_zero_intrinsic_argument (FACTORIAL(ZERO) = 1) and
      *> 2014/pb125_factorial_standard_decimal (FACTORIAL(0) = 1). FACTORIAL(1), and the whole of
      *> r1b - the value 2, which the standard breaks out into a lettered arm of its own - had
      *> never been written down anywhere in the corpus. r1c was covered (2023/pb40 3! = 6,
      *> 2023/pb21 5! = 120, 2002/pb125 33!/31! = 1056, 2014/pb125 34!/33! = 34) and is
      *> re-asserted here so all three arms are read off ONE output.
      *>
      *> The values, hand-multiplied from r1 itself:
      *>   0!  = 1                      (r1a, first value)
      *>   1!  = 1                      (r1a, second value)
      *>   2!  = 2                      (r1b)
      *>   5!  = 5*4*3*2*1 = 120        (r1c)
      *>   20! = 2432902008176640000    (r1c; 19 digits, exact inside the 34-digit SDIDI)
      *>   3!/2! = 6/2 = 3              (r1c over r1b - the arms meeting)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FACTEAE.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC 9(19) VALUE 0.
       01 W-N PIC 9(2) VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-R = FUNCTION FACTORIAL ( 0 ).
           DISPLAY "A-ZERO  =" W-R.
           COMPUTE W-R = FUNCTION FACTORIAL ( 1 ).
           DISPLAY "A-ONE   =" W-R.
           COMPUTE W-R = FUNCTION FACTORIAL ( 2 ).
           DISPLAY "B-TWO   =" W-R.
           COMPUTE W-R = FUNCTION FACTORIAL ( 5 ).
           DISPLAY "C-FIVE  =" W-R.
           COMPUTE W-R = FUNCTION FACTORIAL ( 20 ).
           DISPLAY "C-TWENTY=" W-R.
      *> The arms must meet: r1c's product at n = 3 divided by r1b's value at 2 is 3.
           COMPUTE W-R = FUNCTION FACTORIAL(W-N)
                       / FUNCTION FACTORIAL(W-N - 1).
           DISPLAY "C-RATIO3=" W-R.
           STOP RUN.
