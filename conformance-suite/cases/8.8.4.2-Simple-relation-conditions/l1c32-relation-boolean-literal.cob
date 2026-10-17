*>--- conformance-case
*> id: l1c32-relation-boolean-literal
*> rules: [SR-8.8.4.2.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.4.2.3 SR3 — a boolean literal in a general relation condition
      *>
      *> THE RULE. §8.8.4.2.3 FORMAT 1: "3) All literals shall be of class alphanumeric,
      *> national, or numeric."
      *>   cite.py: OK  §8.8.4.2.3 3)  (Syntax rules)
      *> X1 is class alphanumeric, so `X1 = B"1"` is not a Format 2 (boolean) relation,
      *> whose operands are boolean-expressions; it can only be Format 1, where the boolean
      *> literal B"1" violates SR3. Nothing else in the program is illegal. Boolean
      *> literals exist from ISO/IEC 1989:2002, so the program is rejected from 2002 on.
      *> .err stops before the message's parenthesised citation, which names
      *> "§8.8.4.2.1 F1 SR2/SR3" (the syntax rules are in §8.8.4.2.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1           PIC X VALUE "1".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF X1 = B"1" DISPLAY "EQ" END-IF.
           STOP RUN.
