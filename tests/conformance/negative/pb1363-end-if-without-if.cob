*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.16.2 general format: >> IF, an optional >> ELSE, then >> END-IF
*> (cite.py --check 7.3.16.2 ">> END-IF" -> OK). Fixed form. kb/Work PB1363.
*> A >>END-IF with no open >>IF closes nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N02.
       PROCEDURE DIVISION.
           DISPLAY "A".
       >>END-IF
           STOP RUN.
