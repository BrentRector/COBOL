*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.22.3 SR2 with 7.3.10.3 SR1 (cite.py --check 7.3.10.3 "The COBOL-WORDS directive may be specified only
*> before the first IDENTIFICATION DIVISION within a compilation group" -> OK 1)): a PUSH naming COBOL-WORDS after the first unit
*> began is written where COBOL-WORDS must not be. kb/Work PB1377. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1377N03.
       PROCEDURE DIVISION.
       >>PUSH COBOL-WORDS
           DISPLAY "A".
           STOP RUN.
