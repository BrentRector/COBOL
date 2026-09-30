*> reject-at: 2002 2014 2023
*> 7.3.3 SR3 (cite.py --check 7.3.3 "may be followed only by space characters and an optional inline comment"
*> -> OK 3)): the general format writes >> END-EVALUATE with no operand, so a word after it is neither space nor comment.
*> kb/Work PB806. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB806N03.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>EVALUATE V
       >>WHEN 1
           DISPLAY "W1".
       >>END-EVALUATE JUNK
           STOP RUN.
