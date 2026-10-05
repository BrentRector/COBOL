*> reject-at: 2002 2014 2023
*> 7.3.13.3 SR14 (cite.py --check 7.3.13.3 "Arithmetic-expression-1, arithmetic-expression-2, and arithmetic-expression-3 shall be formed in accordance with 7.3.6, Compile-time arithmetic expressions." -> OK 14)):
*> the exponentiation operator is not a compile-time arithmetic operator (7.3.6.2 SR1a), and the rule holds for the
*> arithmetic-expression of EVERY >>WHEN, not only those evaluated before a match. kb/Work PB1364. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1364N07.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN 1
           DISPLAY "W1".
       >>WHEN 2 ** 3
           DISPLAY "W2".
       >>END-EVALUATE
           STOP RUN.
