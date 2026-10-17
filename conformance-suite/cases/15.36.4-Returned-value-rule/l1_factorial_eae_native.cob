      *>--- conformance-case
      *> id: l1_factorial_eae_native
      *> rules: [RV-15.36.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.36.4 r1 — the same three lettered arms of FACTORIAL's equivalent arithmetic
      *> expression under NATIVE arithmetic, at the EARLIEST edition that has the function.
      *>
      *> --check validated:
      *>   cite.py --check 15.36.4 "The equivalent arithmetic expression is as follows"
      *>     -> OK §15.36.4 1)   a) 0 or 1 -> (1)   b) 2 -> (2)   c) n -> (n * (n-1) * ... * 1)
      *>   cite.py --check 15.36.1 "The type of this function is integer." -> OK §15.36.1
      *>   cite.py --check 15.36.3 "Argument-1 shall be an integer greater than or equal to zero."
      *>     -> OK §15.36.3 1)
      *>   cite.py --check 11.9.5.2 "it is as if the ARITHMETIC clause were specified with the
      *>     NATIVE phrase" -> OK §11.9.5.2 4) - which is what an absent OPTIONS paragraph selects
      *>     and is the ONLY mode reachable at COBOL-85, the OPTIONS paragraph being later.
      *>   §15.2 item 5: "Integer functions. These are of the class and category numeric. An
      *>     integer function has an operational sign and no digits to the right of the decimal
      *>     point." - so the returned value carries no fraction in this mode either.
      *>
      *> ⛔ WHY THE ASSERTIONS ARE RELATIONS AND NOT PRINTED DIGITS. Under native arithmetic
      *> §15.4.1 gives only "the value returned is an implementor-defined approximation of the
      *> value of that expression" (--check OK), so a pinned digit string would be a
      *> determination of this implementation dressed as a rule. Every line below instead relates
      *> TWO evaluations of the rule's OWN expressions, which r1 fixes with respect to each other
      *> however either is approximated: r1a gives 0! and 1! the SAME expression (1); r1b's (2) is
      *> that doubled and is equally r1c's product at n = 2; and r1c's product telescopes, so
      *> n! / (n-1)! is n. The one absolute pin, 1! = 1, is r1a's expression written out - it is
      *> the literal the standard prints, not a rendering.
      *>
      *> ⚠ NO INTRINSIC-FUNCTION GOLDEN EXISTED IN THE 85 CORPUS BEFORE THIS CHANGE SET, which
      *> lands two: this file and 85/l1_acos_domain_accept. FACTORIAL carries IntroducedIn 85 in
      *> IntrinsicCatalog (the 1989 Intrinsic Function Module amendment is part of the 85 dialect
      *> here), and the inventory row spans 85 through 2023, so the edition where the ONLY
      *> available arithmetic mode is the approximating one had no witness. Both files rest on the
      *> same premise - IntrinsicBinder's edition gate is `IntroducedIn > DialectLevel`, which
      *> 85 > 85 does not satisfy - so if a run shows --std 85 gating intrinsics after all, BOTH
      *> move to tests/conformance/2002/ unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FACTNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC 9(4) VALUE 0.
       01 W-N PIC 9(2) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
      *> r1a names 0 AND 1 and gives both the SAME expression, so the two must agree.
           IF FUNCTION FACTORIAL ( 0 ) = FUNCTION FACTORIAL ( 1 )
               DISPLAY "A-0-EQ-1=YES"
           ELSE
               DISPLAY "A-0-EQ-1=NO"
           END-IF.
      *> r1a's expression IS (1).
           IF FUNCTION FACTORIAL ( 1 ) = 1
               DISPLAY "A-ONE=1"
           ELSE
               DISPLAY "A-ONE=BAD"
           END-IF.
      *> r1b's expression IS (2) - the arm the standard prints on its own.
           IF FUNCTION FACTORIAL ( 2 ) = 2
               DISPLAY "B-TWO=2"
           ELSE
               DISPLAY "B-TWO=BAD"
           END-IF.
      *> r1b's (2) is r1a's (1) doubled, and equally r1c's product at n = 2.
           IF FUNCTION FACTORIAL ( 2 ) = FUNCTION FACTORIAL ( 1 ) * 2
               DISPLAY "B-EQ-2A=YES"
           ELSE
               DISPLAY "B-EQ-2A=NO"
           END-IF.
      *> r1c's product telescopes: 5! / 4! = 5.
           COMPUTE W-R = FUNCTION FACTORIAL ( 5 )
                       / FUNCTION FACTORIAL ( 4 ).
           IF W-R = 5
               DISPLAY "C-RATIO5=YES"
           ELSE
               DISPLAY "C-RATIO5=NO"
           END-IF.
      *> ...and again at the boundary between r1c and r1b, from an arithmetic-expression
      *> argument (§15.3 type 6 admits one that always results in an integer): 3! / 2! = 3.
           COMPUTE W-R = FUNCTION FACTORIAL ( W-N - 2 )
                       / FUNCTION FACTORIAL ( W-N - 3 ).
           IF W-R = 3
               DISPLAY "C-RATIO3=YES"
           ELSE
               DISPLAY "C-RATIO3=NO"
           END-IF.
           STOP RUN.
