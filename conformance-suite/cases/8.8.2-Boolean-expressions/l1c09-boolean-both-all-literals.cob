      *>--- conformance-case
      *> id: l1c09-boolean-both-all-literals
      *> rules: [SR-8.8.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.8.2 SR4 — both operands of a binary boolean operation
      *> are the figurative constant ALL literal.
      *> Rule: "The two operands in a binary boolean operation shall
      *> not both be the figurative constant ALL literal."
      *> cite.py: OK  §8.8.2 4)  (Boolean expressions)
      *> The COMPUTE below is otherwise legal (B-AND of two boolean
      *> operands into a boolean receiver); the only defect is that
      *> BOTH operands of B-AND are ALL literals. Its sibling line with
      *> ONE ALL literal (B1 B-AND ALL B"0") is legal and must not be
      *> what triggers the rejection.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C09N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1(4) VALUE B"1010".
       01 B2 PIC 1(4).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE B2 = B1 B-AND ALL B"0".
           COMPUTE B2 = ALL B"1" B-AND ALL B"0".
           DISPLAY B2.
           STOP RUN.
