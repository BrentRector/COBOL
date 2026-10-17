      *>--- conformance-case
      *> id: l1c19-multiply-composite-giving-counted
      *> rules: [SR-14.9.26.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.26.3 4) — MULTIPLY's native composite counts the
      *>   GIVING operand
      *> "When native arithmetic is in effect, the composite of operands
      *>   described
      *> in 14.7.7, Arithmetic statements, is determined by using all of
      *>   the
      *> operands in the statement."
      *> OK  §14.9.26.3 4)  (Syntax rules)
      *> 14.7.7 2) a): "the composite of operands shall not contain more
      *>   than 31
      *> digits."  OK  §14.7.7 2) a)  (Arithmetic statements)
      *> 11.9.5.2 4): "If the ARITHMETIC clause is not specified ... it
      *>   is as if
      *> the ARITHMETIC clause were specified with the NATIVE phrase."
      *> OK  §11.9.5.2 4)  (General rules)
      *> No ARITHMETIC clause, so native arithmetic is in effect.
      *> A is V9(12) (0 integer, 12 decimal digits), B is 9(10).
      *> MULTIPLY A BY B GIVING R19 (R19 is 9(19)): composite = 19
      *>   integer + 12
      *> decimal = 31 digits - valid.
      *> MULTIPLY A BY B GIVING R20 (R20 is 9(20)): composite = 20 + 12
      *>   = 32 > 31
      *> ONLY because the GIVING operand is one of "all of the operands"
      *>   - the
      *> sending operands alone are 10 + 12 = 22. The source shall be
      *>   rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC V9(12) VALUE .5.
       01 B PIC 9(10) VALUE 2.
       01 R19 PIC 9(19).
       01 R20 PIC 9(20).
       PROCEDURE DIVISION.
           MULTIPLY A BY B GIVING R19
           MULTIPLY A BY B GIVING R20
           STOP RUN.
