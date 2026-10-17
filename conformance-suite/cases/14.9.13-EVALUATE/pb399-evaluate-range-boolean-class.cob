*>--- conformance-case
*> id: pb399-evaluate-range-boolean-class
*> rules: [SR-14.9.13.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR4's first excluded class: "The two operands in a range-expression shall be of the
      *> same class and shall not be of class boolean, message-tag, object, or pointer."  A PIC 1 item is of
      *> class and category boolean (§8.5.2.1 Table 2), and §8.8.4.2.2 Format 2 admits boolean operands for
      *> EQUALITY only — a range has no boolean meaning to lower to.
      *>
      *> ⚠ The operand rule and the COMPARISON rule are two different rules and this case is the operand
      *> one.  The relation checkpoint's COBOLNET0844 ("boolean operands compare for equality only") is
      *> about a comparison that has already been admitted; SR4 says the range may not be WRITTEN, which is
      *> one phase earlier and does not depend on which of the two lowerings the range happens to take
      *> (an inverted or IN-alphabet range builds a membership node and reaches no relation at all).
      *> PIC 1 / USAGE BIT is a COBOL-2002 introduction.
      *>
      *> ⚠ THE SELECTION SUBJECT IS AN IDENTIFIER, NOT A BOOLEAN ONE, ON PURPOSE.  Table 15's
      *> `[NOT] range-expression` row is blank under the Boolean-expression COLUMN, so a boolean SUBJECT
      *> against a range object is independently invalid under SR10 and would draw COBOLNET1634 as well —
      *> the case would then reject for a rule that is not the one under test
      *> (feedback_green_gates_arent_evidence).  Identifier x range-expression is 'Y', so the only rule
      *> left to catch this program is SR4's exclusion of the range OPERANDS' class.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399RBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X  PIC X(2) VALUE "01".
       01 WS-LO PIC 1(2) VALUE B"00".
       01 WS-HI PIC 1(2) VALUE B"11".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-X
               WHEN WS-LO THRU WS-HI
                   DISPLAY "BOOL-MATCHED"
               WHEN OTHER
                   DISPLAY "BOOL-OTHER"
           END-EVALUATE.
           STOP RUN.
