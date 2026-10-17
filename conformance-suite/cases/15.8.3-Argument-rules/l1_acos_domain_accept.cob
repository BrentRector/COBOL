      *>--- conformance-case
      *> id: l1_acos_domain_accept
      *> rules: [AR-15.8.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.8.3 r2 — the ACCEPT arm of ACOS's argument VALUE domain, at the EARLIEST edition
      *> that has the function. The closed interval [-1, +1] is admitted at BOTH endpoints.
      *>
      *> THE RULE, --check validated:
      *>   cite.py --check 15.8.3 "The value of argument-1 shall be greater than or equal to"
      *>     -> OK §15.8.3 2)  "2) The value of argument-1 shall be greater than or equal to -1
      *>        and less than or equal to +1."
      *>   (⚠ NOT r1. §15.8.3 r1 is "Argument-1 shall be of class numeric" - a different rule.)
      *>   cite.py --check 15.8.4 "The returned value is the approximation of the arccosine of
      *>     argument-1 and is greater than or equal to zero and less than or equal to"
      *>     -> OK §15.8.4 1) - the codomain is [0, pi].
      *>   cite.py --check 15.4.1 "When native arithmetic is in effect, the characteristics and
      *>     representation of the returned value are defined by the implementor" -> OK
      *>   cite.py --check 11.9.5.2 "it is as if the ARITHMETIC clause were specified with the
      *>     NATIVE phrase" -> OK §11.9.5.2 4) - and at COBOL-85 the OPTIONS paragraph does not
      *>     exist at all, so the approximating mode is the ONLY one reachable here.
      *>
      *> ⛔ WHY ONLY THE ACCEPT ARM AT THIS EDITION. §15.8.3 r2's REJECT arm has its consequence in
      *> §15.3 item 14 - "the EC-ARGUMENT-FUNCTION exception condition is set to exist" (--check
      *> OK; cite.py reports the ITEM number, recorded here rather than a bare § because an
      *> inherited clause reference that names no item is how a wrong number propagates) - and
      *> that paragraph's closing sentence hands the RESULT to the implementor whenever checking
      *> for EC-ARGUMENT-FUNCTION is not enabled. Enabling it needs >>TURN and the exception-name
      *> declarative, both of which are the 2002+ EC model: below --std 2002 a >>TURN is the hard
      *> COBOLNET0900. So at COBOL-85 the reject arm has NO spec-fixed observable by any test, and
      *> the arm that IS observable - r2's admission of the endpoints, ACOS being available from
      *> 85 - is what this file measures. 2023/l1_acos_argument_domain carries both arms.
      *>
      *> The values are the mathematical arccosine the rule names, inside §15.8.4 r1's codomain:
      *> arccos(+1) = 0, arccos(-1) = pi = 3.14159265358979..., arccos(0) = pi/2 =
      *> 1.57079632679489... Each is pinned by an INCLUSIVE 1E-6 window rather than by printing
      *> this implementation's own digits back at itself, because §15.4.1 makes a native returned
      *> value an implementor-defined approximation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ACOSACC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC S9V9(6) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
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
           STOP RUN.
