*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> The optional >> WHEN OTHER comes after every >> WHEN, immediately before >> END-EVALUATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N09.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN OTHER
           DISPLAY "O1".
       >>WHEN 1
           DISPLAY "W1".
       >>END-EVALUATE
           STOP RUN.
