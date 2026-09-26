      *> reject-at: 85 2002 2014 2023
      *> ISO §8.8.4.12.3 SR2 — the RESULT OF IMPLIED INSERTION in an
      *> abbreviated combined relation condition shall comply with
      *> Table 5; an abbreviation whose expansion yields 'NOT NOT' is
      *> refused.
      *>   cite.py --check 8.8.4.12.3 "The result of implied insertion
      *>     shall comply with the rules of Table 5, Combinations of
      *>     conditions, logical operators, and parentheses." -> OK 2)
      *>   cite.py --check 8.8.4.12.4 "The effect of using abbreviations
      *>     is as if the last preceding stated subject were inserted in
      *>     place of the omitted subject, and the last stated relational
      *>     operator were inserted in place of the omitted relational
      *>     operator." -> OK 1)
      *>   cite.py --check 8.8.4.11.3 "the pair 'NOT NOT' is not
      *>     permissible" -> OK (Table 5 NOTE)
      *> DERIVATION. In 'A = B AND NOT NOT C' the operand C has neither
      *> subject nor relational operator, so it is the abbreviated form
      *> whose subject (A) and operator (=) are inserted (GR1); the
      *> nearer NOT is the abbreviation's own NOT (the 8.8.4.12.2 format
      *> admits NOT before object-1) and the farther NOT can only be the
      *> logical operator of the enclosing complex condition. The result
      *> of insertion is 'A = B AND NOT NOT A = C'. Table 5's NOT row
      *> admits only "simple-condition, (" after a NOT, and its NOTE
      *> names 'NOT NOT' as not permissible, so SR2 is violated and the
      *> source must be rejected at every edition (a syntax error, the
      *> generic parse diagnostic COBOL0001).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ABNN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 1.
       01 C PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A = B AND NOT NOT C
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF.
           STOP RUN.
