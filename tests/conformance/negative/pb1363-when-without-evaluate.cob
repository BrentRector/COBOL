*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> A >>WHEN with no open >>EVALUATE belongs to no directive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N06.
       PROCEDURE DIVISION.
       >>WHEN 1
           DISPLAY "W1".
           STOP RUN.
