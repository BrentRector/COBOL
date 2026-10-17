      *>--- conformance-case
      *> id: pb158-stacked-unary-literal
      *> rules: [GR-8.8.1.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE SECOND SIGN SOURCE, AND THE ONE A GRAMMAR-ONLY FIX WOULD
      *> LEAVE OPEN. 'signedNumericLiteral : (PLUS|MINUS)? ...' MEANS
      *> A SIGN CAN REACH AN OPERAND WITHOUT GOING THROUGH
      *> unaryExpression's addOp, so tightening that rule alone would
      *> still admit this. IT IS A TABLE 3 VIOLATION BECAUSE THE
      *> SECOND SIGN IS SEPARATED FROM THE DIGITS: 8.3.3.3.2 rule 2
      *> makes a numeric literal "a character-string" whose sign is
      *> "the leftmost character of the literal", and a space is a
      *> separator (8.3.5) - so '- 2' is a unary operator applied to
      *> 2, not the literal -2. THE ADJACENT SPELLING '- -2' IS
      *> PERMISSIBLE and is pinned in the positive golden
      *> pb158_arith_precedence_spine (R12). kb/Work PB158.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB158N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(6) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = - - 2.
           STOP RUN.
