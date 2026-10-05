*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.10.3 SR4 (cite.py --check 7.3.10.3 "shall be a COBOL word that meets the requirements for a user-defined
*> data-name" -> OK 4)) with 8.3.2.1 ("not more than 63 characters"): a 64-character fresh word is no user-defined word. A word
*> no statement uses never reaches the tree funnel that asks the 63-character ceiling of the rest, so this directive asks it.
*> Free form (the operand is wider than margin R). kb/Work PB1373.
>>SOURCE FORMAT FREE
>>COBOL-WORDS RESERVE "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKL"
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1373N04.
PROCEDURE DIVISION.
    DISPLAY "A".
    STOP RUN.
