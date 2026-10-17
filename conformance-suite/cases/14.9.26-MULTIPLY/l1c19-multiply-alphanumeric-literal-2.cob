      *>--- conformance-case
      *> id: l1c19-multiply-alphanumeric-literal-2
      *> rules: [SR-14.9.26.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.26.3 3) — a MULTIPLY literal-2 (format 2) that is not
      *>   numeric
      *> "Literal-1 and literal-2 shall be numeric literals."
      *> OK  §14.9.26.3 3)  (Syntax rules)
      *> Format 2 (GIVING): the BY operand "3" is an alphanumeric
      *>   literal used as
      *> literal-2 - the source shall be rejected in every edition. The
      *>   numeric
      *> literal 3 in the same position is valid.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 4.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           MULTIPLY A BY 3 GIVING R
           MULTIPLY A BY "3" GIVING R
           STOP RUN.
