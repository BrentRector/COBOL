*> reject-at: 2002 2014 2023
*> 7.3.13.3 SR11 (cite.py --check 7.3.13.3 "All operands of one EVALUATE directive shall be of the same category."
*> -> OK 11)) and SR4/SR5/SR6: syntax rules hold for EVERY >>WHEN of the directive, not only for those evaluated
*> before a match. kb/Work PB1364. Fixed form.
*> Format 2: a later constant-conditional-expression is formed per 7.3.8 whether or not an earlier WHEN matched.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1364N04.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>EVALUATE TRUE
       >>WHEN V = 1
           DISPLAY "W1".
       >>WHEN V = "A"
           DISPLAY "WA".
       >>END-EVALUATE
           STOP RUN.
