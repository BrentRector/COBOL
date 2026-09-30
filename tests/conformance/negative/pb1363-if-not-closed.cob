*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.16.2 general format: >> IF, an optional >> ELSE, then >> END-IF
*> (cite.py --check 7.3.16.2 ">> END-IF" -> OK). Fixed form. kb/Work PB1363.
*> The >>IF is never closed: >> END-IF is not optional in the format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N04.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 1
           DISPLAY "A".
           STOP RUN.
