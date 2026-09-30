*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> 7.3.13.3 SR9 (cite.py --check 7.3.13.3 "the phrases of a given EVALUATE directive shall all be specified
*> in the same library text or all in source-text" -> OK 9)): the EVALUATE opens in source text, closes in the copybook.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N14.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>EVALUATE V
       >>WHEN 1
           DISPLAY "W1".
           COPY PB1363EV.
           STOP RUN.
