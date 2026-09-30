*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> [ >> WHEN OTHER [ text-2 ] ] is ONE optional phrase: a second one is not in the format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N08.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN 2
           DISPLAY "W2".
       >>WHEN OTHER
           DISPLAY "O1".
       >>WHEN OTHER
           DISPLAY "O2".
       >>END-EVALUATE
           STOP RUN.
