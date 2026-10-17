*>--- conformance-case
*> id: l1_acos_argument_domain
*> rules: [AR-15.8.3-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.8.3 r2 — ACOS argument-1 VALUE domain: the closed interval [-1, +1] is admitted at
      *> BOTH endpoints, and a value outside it sets the EC-ARGUMENT-FUNCTION exception condition.
      *>
      *> THE RULE, --check validated:
      *>   cite.py --check 15.8.3 "The value of argument-1 shall be greater than or equal to"
      *>     -> OK §15.8.3 2)  "2) The value of argument-1 shall be greater than or equal to -1
      *>        and less than or equal to +1."
      *>   (⚠ NOT r1. §15.8.3 r1 is "Argument-1 shall be of class numeric" - a different rule,
      *>    already witnessed by negative/pb1-numeric-arg-trig-family. The r1/r2 confusion is live
      *>    in the corpus: 2023/pb13_domain_raise_receiver_shape cites "15.8.3 r1" for THIS value
      *>    rule at its line 17 and "15.84.3 r1" for SQRT's value rule at its line 37, where the
      *>    standard's value rules are §15.8.3 r2 and §15.84.3 r2.)
      *>
      *> THE CONSEQUENCE, --check validated:
      *>   cite.py --check 15.3 "the EC-ARGUMENT-FUNCTION exception condition is set to exist"
      *>     -> OK §15.3 14) (Arguments): "The rules for a function may place constraints on the
      *>        permissible values for arguments ... If the evaluation of an argument results in
      *>        an incorrect value for that argument ... the EC-ARGUMENT-FUNCTION exception
      *>        condition is set to exist."
      *>   cite.py --check 15.3 "the implementor defines the result of the function reference"
      *>     -> OK §15.3 14), the same numbered paragraph, closing sentence.
      *>     (cite.py reports the ITEM number, 14) - recorded here rather than the bare clause,
      *>      because an inherited § that names no item is how a wrong number propagates.)
      *>
      *> THE RETURNED VALUES, --check validated:
      *>   cite.py --check 15.8.4 "The returned value is the approximation of the arccosine of
      *>     argument-1 and is greater than or equal to zero and less than or equal to"
      *>     -> OK §15.8.4 1) - the codomain is [0, pi].
      *>
      *> ⛔ WHY CHECKING IS TURNED ON, AND WHY NO EXISTING GOLDEN CLOSES THIS ROW. §15.3's closing
      *> sentence makes the RESULT implementor-defined while checking for EC-ARGUMENT-FUNCTION is
      *> not enabled, so with checking OFF this rule has no spec-fixed observable at all -
      *> 2023/pb13_domain_raise_receiver_shape pins the implementor default (0) and the agreement
      *> of the receiver-ful and receiver-less shapes, which is a determination about §15.4, not
      *> this rule. Only with checking ON does the standard fix what must be observed: the
      *> exception condition EXISTS, so the EC-ARGUMENT-FUNCTION declarative runs.
      *>
      *> ⛔ AND WHY THIS FILE CANNOT CARRY EDITION 85. >>TURN and the exception-name declarative are
      *> the 2002+ EC model: below --std 2002 a >>TURN is the hard COBOLNET0900, so this program is
      *> not compilable at 85 at all. The row spans 85 through 2023 and ACOS is available from 85,
      *> so the arm that IS observable there - r2's admission of the endpoints - is carried by the
      *> companion 85/l1_acos_domain_accept, and r2's REJECT arm has no spec-fixed observable at
      *> COBOL-85 by any test whatever.
      *>
      *> ⛔ BOTH ARMS, AND BOTH ENDPOINTS. A closed-interval rule is falsified from two directions:
      *> by raising on -1 or +1 (over-rejecting legal source, the shape §15.8.4 r1 then has no
      *> value for) and by staying silent above +1 or below -1. Lines 1-3 are the ACCEPT arm and
      *> 4-7 the REJECT arm. Lines 6-7 sit one unit in the ninth decimal outside each endpoint,
      *> from a runtime data item rather than a literal, so the boundary is measured AT exactly
      *> +/-1 and no constant fold can see the violation.
      *>
      *> The accept arm's values are the mathematical arccosine the rule names, inside §15.8.4
      *> r1's codomain: arccos(+1) = 0, arccos(-1) = pi = 3.14159265358979..., arccos(0) = pi/2 =
      *> 1.57079632679489... §15.4.1 makes a native returned value an implementor-defined
      *> approximation ("When native arithmetic is in effect, the characteristics and
      *> representation of the returned value are defined by the implementor" -- --check OK), so
      *> each is pinned by an inclusive 1E-6 WINDOW - the general_format_intrinsics_batch1
      *> convention - and never by printing this implementation's own digits back at itself.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ACOSDOM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC S9V9(6) VALUE 0.
       01 W-A PIC S9V9(9) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  RAISED".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> 1 - the UPPER endpoint, which r2 admits ("less than or equal to +1"). arccos(1) = 0.
           DISPLAY "1-AT-PLUS1".
           COMPUTE W-R = FUNCTION ACOS(1).
           IF W-R >= 0 AND W-R <= 0.000001
               DISPLAY "  V=ZERO"
           ELSE
               DISPLAY "  V=BAD"
           END-IF.
      *> 2 - the LOWER endpoint ("greater than or equal to -1"). arccos(-1) = pi.
           DISPLAY "2-AT-MINUS1".
           COMPUTE W-R = FUNCTION ACOS(-1).
           IF W-R >= 3.141592 AND W-R <= 3.141593
               DISPLAY "  V=PI"
           ELSE
               DISPLAY "  V=BAD"
           END-IF.
      *> 3 - the interior. arccos(0) = pi/2.
           DISPLAY "3-INTERIOR".
           COMPUTE W-R = FUNCTION ACOS(0).
           IF W-R >= 1.570796 AND W-R <= 1.570797
               DISPLAY "  V=HALFPI"
           ELSE
               DISPLAY "  V=BAD"
           END-IF.
      *> 4 - above the upper bound, from a literal.
           DISPLAY "4-ABOVE-PLUS1".
           COMPUTE W-R = FUNCTION ACOS(2).
      *> 5 - below the lower bound, from a literal.
           DISPLAY "5-BELOW-MINUS1".
           COMPUTE W-R = FUNCTION ACOS(-2).
      *> 6 - one unit in the ninth decimal ABOVE +1, from a runtime data item.
           DISPLAY "6-EDGE-ABOVE".
           MOVE 1.000000001 TO W-A.
           COMPUTE W-R = FUNCTION ACOS(W-A).
      *> 7 - the same distance BELOW -1, from a runtime data item.
           DISPLAY "7-EDGE-BELOW".
           MOVE -1.000000001 TO W-A.
           COMPUTE W-R = FUNCTION ACOS(W-A).
           DISPLAY "DONE".
           STOP RUN.
