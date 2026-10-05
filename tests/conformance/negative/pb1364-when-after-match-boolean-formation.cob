*> reject-at: 2002 2014 2023
*> 7.3.13.3 SR15 (cite.py --check 7.3.13.3 "Boolean-expression-1 and boolean-expression-2 shall be formed in accordance with 7.3.7, Compile-time boolean expressions." -> OK 15)):
*> a boolean operator takes boolean operands (7.3.7.2 SR1), and the rule holds for the boolean-expression of EVERY
*> >>WHEN, not only those evaluated before a match. kb/Work PB1364. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1364N08.
       PROCEDURE DIVISION.
       >>EVALUATE B"1"
       >>WHEN B"1"
           DISPLAY "W1".
       >>WHEN B"1" B-AND 1
           DISPLAY "W2".
       >>END-EVALUATE
           STOP RUN.
