      *>--- conformance-case
      *> id: l1c19-multiply-alphanumeric-literal
      *> rules: [SR-14.9.26.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.26.3 3) — a MULTIPLY literal operand that is not
      *>   numeric
      *> "Literal-1 and literal-2 shall be numeric literals."
      *> OK  §14.9.26.3 3)  (Syntax rules)
      *> "12" is an alphanumeric literal (8.3.3.2), not a numeric
      *>   literal, used as
      *> literal-1 of format 1 - the source shall be rejected in every
      *>   edition.
      *> The numeric literal 12 in the same position is valid.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 9(4) VALUE 4.
       PROCEDURE DIVISION.
           MULTIPLY 12 BY B
           MULTIPLY "12" BY B
           STOP RUN.
