*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.10.3 SR2 (cite.py --check 7.3.10.3 "Each literal shall be an alphanumeric literal" -> OK 2)):
*> an operand with no closing quotation symbol is no alphanumeric literal. It used to be read as one, to the end of the line,
*> and the word FOO was reserved. kb/Work PB1373.
       >>COBOL-WORDS RESERVE "FOO
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1373N01.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
