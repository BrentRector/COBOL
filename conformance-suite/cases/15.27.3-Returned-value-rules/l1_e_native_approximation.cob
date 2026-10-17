*>--- conformance-case
*> id: l1_e_native_approximation
*> rules: [RV-15.27.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.27.3 r1 — FUNCTION E under NATIVE arithmetic: the returned value is an
      *> implementor-defined approximation of (2 + 0.71828182845904523536028747135 26).
      *>
      *> --check validated:
      *>   cite.py --check 15.27.3 "If native arithmetic is in effect, the returned value is an
      *>     implementor-defined approximation" -> OK §15.27.3 1)
      *>   cite.py --check 11.9.5.2 "it is as if the ARITHMETIC clause were specified with the
      *>     NATIVE phrase" -> OK §11.9.5.2 4) - so OMITTING the ARITHMETIC clause is precisely
      *>     what selects r1's arm. This program has no OPTIONS paragraph for that reason.
      *>   cite.py --check 15.4.1 "When native arithmetic is in effect, the characteristics and
      *>     representation of the returned value are defined by the implementor" -> OK
      *>   cite.py --check 15.34.4 "(FUNCTION E \*\* (argument-1))" -> OK §15.34.4 1)
      *>   cite.py --check 15.4 "The evaluation of a function produces a returned value in a
      *>     temporary elementary data item" -> OK §15.4
      *>
      *> ⛔ THE ARITHMETIC MODE IS THE BRANCH, and it is the arm the corpus did not have. §15.27.3
      *> has three rules, one per mode, and only two were witnessed: 2023/exp_standard_decimal_eae
      *> and 2023/pb65_dec_move_channel pin r3 (STANDARD-DECIMAL, where the value is EXACT), and
      *> negative/standard-binary-e-2014 / -2002 pin the r2 arm's decline and its edition gate.
      *> r1 - the DEFAULT mode, the one every program without an OPTIONS paragraph gets - had no
      *> golden at all, so the rule that governs the ordinary case was the untested one.
      *>
      *> ⚠ WHAT MAY AND MAY NOT BE ASSERTED HERE. r1 fixes WHAT is approximated and says nothing
      *> about HOW CLOSELY; §15.4.1 then leaves the representation to the implementor. So the
      *> value is pinned by WINDOWS around the constant the rule itself writes, never by printing
      *> this implementation's digits back at itself. And §15.4.1's sentence "the returned value
      *> is the same for all instances of a given function within a single execution" is NOT
      *> available here: its paragraph opens "When standard-decimal arithmetic or standard-binary
      *> arithmetic is in effect", so it answers a different question than the native mode's.
      *>
      *> The constant: 2 + 0.71828182845904523536028747135 26 = 2.71828182845904523536...
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ENAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E PIC 9V9(9) VALUE 0.
       01 W-X PIC 9V9(9) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> 1 - the rule writes the constant as (2 + 0.718...), so the value lies strictly between
      *> 2 and 3. This half of the claim needs no accuracy assumption whatever.
           COMPUTE W-E = FUNCTION E.
           IF W-E > 2 AND W-E < 3
               DISPLAY "INTERVAL=IN"
           ELSE
               DISPLAY "INTERVAL=OUT"
           END-IF.
      *> 2 - and a 1E-7 window about it, the corpus convention for a §15.4.1 approximation.
           IF W-E > 2.7182818 AND W-E < 2.7182819
               DISPLAY "WINDOW=IN"
           ELSE
               DISPLAY "WINDOW=OUT"
           END-IF.
      *> 3 - the same reference with NO receiver. §15.4 puts the returned value in a temporary
      *> elementary data item carrying the FUNCTION's own characteristics, so where the value is
      *> going cannot be what makes it an approximation of e.
           IF FUNCTION E > 2.7182818 AND FUNCTION E < 2.7182819
               DISPLAY "BARE=IN"
           ELSE
               DISPLAY "BARE=OUT"
           END-IF.
      *> 4 - §15.34.4 r1 makes EXP's equivalent arithmetic expression (FUNCTION E ** argument-1),
      *> so at argument-1 = 1 that expression IS FUNCTION E: two bodies, one constant.
           COMPUTE W-X = FUNCTION EXP(1).
           IF W-X > 2.7182818 AND W-X < 2.7182819
               DISPLAY "EXP1=IN"
           ELSE
               DISPLAY "EXP1=OUT"
           END-IF.
           STOP RUN.
