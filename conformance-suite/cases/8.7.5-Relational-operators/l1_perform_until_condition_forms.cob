      *>--- conformance-case
      *> id: l1_perform_until_condition_forms
      *> rules: [SR-8.7.5.2-2, SR-8.7.5.2-4, SR-14.9.28.3-7,
      *>    FMT-8.8.4.5.2, FMT-8.8.4.10.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR7 — "Condition-1, condition-2, … , may be any
      *> conditional expression. (See 8.8.4, Conditional expressions.)"
      *>
      *> This is a PERMISSIVE rule: it does not restrict the UNTIL
      *> slot, it opens it to the WHOLE of §8.8.4. A rule of that shape
      *> is discharged by showing the position is not narrowed, so each
      *> leg below writes a DIFFERENT §8.8.4 form into an UNTIL slot
      *> and prints a value derived from that form's own rules. A slot
      *> narrowed to, say, a simple relation condition would fail to
      *> compile these; a slot that accepted them but evaluated them
      *> wrongly would print a different number. Both failure modes are
      *> visible, which is why every leg prints a COUNT or a settled
      *> operand value rather than an OK/BAD verdict.
      *>
      *> The last leg exercises condition-2, which SR7 names alongside
      *> condition-1, in the AFTER phrase of a varying-phrase.
      *>
      *> The rule is worded identically in COBOL-85/2002/2014/2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFCF07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A       PIC 9     VALUE 0.
       01 B       PIC 9     VALUE 0.
       01 A2      PIC 9     VALUE 0.
       01 A3      PIC 9     VALUE 0.
       01 ST      PIC 9     VALUE 0.
          88 ST-DONE        VALUE 1.
       01 C-CN    PIC 9     VALUE 0.
       01 SNEG    PIC S9(3) VALUE -3.
       01 SPOS    PIC S9(3) VALUE -3.
       01 C-SGN1  PIC 9     VALUE 0.
       01 C-SGN2  PIC 9     VALUE 0.
       01 CX      PIC X(3)  VALUE "123".
       01 C-CLS   PIC 9     VALUE 0.
       01 EA      PIC 9     VALUE 0.
       01 EB      PIC 9     VALUE 5.
       01 NA      PIC 9     VALUE 0.
       01 VI      PIC 9     VALUE 0.
       01 VJ      PIC 9     VALUE 0.
       01 C-AF    PIC 99    VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      *> (a) §8.8.4.11 complex combined condition with parentheses.
      *> B stays 0, so (A > 2 AND B = 0) OR A > 5 first holds when
      *> A reaches 3 — the loop stops there and A prints 3.
           PERFORM UNTIL (A > 2 AND B = 0) OR A > 5
               ADD 1 TO A
           END-PERFORM.
           DISPLAY "SR7-COMPLEX=" A.
      *> (b) §8.8.4.12 abbreviated combined relation condition.
      *> §8.8.4.12.4 GR1: "the last preceding stated subject were
      *> inserted in place of the omitted subject", so A2 = 1 OR 2 OR 3
      *> is (A2 = 1) OR (A2 = 2) OR (A2 = 3). False at 0, true at 1.
           PERFORM UNTIL A2 = 1 OR 2 OR 3
               ADD 1 TO A2
           END-PERFORM.
           DISPLAY "SR7-ABBREV=" A2.
      *> (c) the same abbreviation carrying NOT — the form §8.8.4.12.4's
      *> own NOTE table expands as "a > b AND NOT < c" ->
      *> "((a > b) AND (a NOT < c))". So A3 NOT < 2 AND NOT > 9 is
      *> (A3 NOT < 2) AND (A3 NOT > 9): false at 0 and 1 (both ARE
      *> less than 2), true at 2 — which is neither less than 2 nor
      *> greater than 9. A3 prints 2.
           PERFORM UNTIL A3 NOT < 2 AND NOT > 9
               ADD 1 TO A3
           END-PERFORM.
           DISPLAY "SR7-ABBNOT=" A3.
      *> (d) §8.8.4.5 simple condition-name condition. ST-DONE is true
      *> when ST holds 1, so exactly one pass.
           PERFORM UNTIL ST-DONE
               ADD 1 TO C-CN
               ADD 1 TO ST
           END-PERFORM.
           DISPLAY "SR7-CONDNM=" C-CN.
      *> (e) §8.8.4.7 simple sign condition, TRUE at entry. §8.8.4.7.4
      *> GR1 b): NEGATIVE "is true if the value is less than zero", and
      *> SNEG holds -3, so §14.9.28.4 GR10 passes control straight to
      *> the end of the PERFORM — zero passes.
           PERFORM UNTIL SNEG IS NEGATIVE
               ADD 1 TO C-SGN1
           END-PERFORM.
           DISPLAY "SR7-SIGN-T=" C-SGN1.
      *> (f) the same sign condition under NOT, FALSE at entry: -3, -2
      *> and -1 are each less than zero, 0 is not, so three passes.
           PERFORM UNTIL SPOS IS NOT NEGATIVE
               ADD 1 TO C-SGN2
               ADD 1 TO SPOS
           END-PERFORM.
           DISPLAY "SR7-SIGN-F=" C-SGN2.
      *> (g) §8.8.4.4 class condition inside a §8.8.4.10 complex
      *> negated condition. §8.8.4.4.4 GR3 n) 2.: for an item whose
      *> category is NOT numeric the NUMERIC test "is true if the
      *> content … consists entirely of the characters 0, 1, 2, 3, …,
      *> 9". "123" does, "12A" does not, so exactly one pass.
           PERFORM UNTIL NOT (CX IS NUMERIC)
               ADD 1 TO C-CLS
               MOVE "12A" TO CX
           END-PERFORM.
           DISPLAY "SR7-CLASS =" C-CLS.
      *> (h) §8.8.4.2 relation condition with an ARITHMETIC EXPRESSION
      *> on both sides. EB is 5, so EA + 1 > EB - 1 is EA + 1 > 4,
      *> first true when EA reaches 4.
           PERFORM UNTIL EA + 1 > EB - 1
               ADD 1 TO EA
           END-PERFORM.
           DISPLAY "SR7-ARITH =" EA.
      *> (i) nested parentheses (§8.8.4.11.3). ((NA > 1) AND ((NA = 2)
      *> OR (NA = 4))) first holds at 2.
           PERFORM UNTIL ((NA > 1) AND ((NA = 2) OR (NA = 4)))
               ADD 1 TO NA
           END-PERFORM.
           DISPLAY "SR7-NESTED=" NA.
      *> (j) CONDITION-2 — SR7 names it, so the AFTER slot is measured
      *> too, with an abbreviated combined relation in it. VJ = 2 OR 3
      *> is (VJ = 2) OR (VJ = 3), so the inner level admits VJ = 1
      *> only; §14.9.28.4 GR13 e) runs the body once per outer value
      *> and the outer level admits VI = 1 and 2. Two body executions.
           PERFORM VARYING VI FROM 1 BY 1 UNTIL VI > 2
                   AFTER VJ FROM 1 BY 1 UNTIL VJ = 2 OR 3
               ADD 1 TO C-AF
           END-PERFORM.
           DISPLAY "SR7-COND-2=" C-AF.
           STOP RUN.
