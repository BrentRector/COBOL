*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.16.2 general format: >> IF, an optional >> ELSE, then >> END-IF
*> (cite.py --check 7.3.16.2 ">> END-IF" -> OK). Fixed form. kb/Work PB1363.
*> 7.3.16.3 SR7 (cite.py --check 7.3.16.3 "The phrases of a given IF directive shall be specified all in
*> the same library text or all in source-text." -> OK 7)): the IF opens in the copybook and closes in source text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N13.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
           COPY PB1363IF.
           DISPLAY "MID".
       >>END-IF
           STOP RUN.
