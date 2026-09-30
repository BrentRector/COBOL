*> reject-at: 2002 2014 2023
*> 7.3.3 SR3 (cite.py --check 7.3.3 "may be followed only by space characters and an optional inline comment"
*> -> OK 3)): the general format writes >> ELSE (inside an omitted branch: the directive is still the frame's structure) with no operand, so a word after it is neither space nor comment.
*> kb/Work PB806. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB806N04.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 2
           DISPLAY "A".
       >>ELSE JUNK
           DISPLAY "B".
       >>END-IF
           STOP RUN.
