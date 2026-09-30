*> reject-at: 2002 2014 2023
*> 7.3.13.3 SR11 (cite.py --check 7.3.13.3 "All operands of one EVALUATE directive shall be of the same category."
*> -> OK 11)) and SR4/SR5/SR6: syntax rules hold for EVERY >>WHEN of the directive, not only for those evaluated
*> before a match. kb/Work PB1364. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1364N01.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN 1
           DISPLAY "W1".
       >>WHEN "A"
           DISPLAY "WA".
       >>END-EVALUATE
           STOP RUN.
