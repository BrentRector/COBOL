*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> >> ELSE is a phrase of the IF directive only; the innermost open directive is an EVALUATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N05.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>EVALUATE V
       >>WHEN 1
           DISPLAY "W1".
       >>ELSE
           DISPLAY "EL".
       >>END-EVALUATE
           STOP RUN.
