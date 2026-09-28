*> reject-at: 85 2002 2014 2023
*> ISO §7.2.4.3 SR2 "A REPLACE statement shall be preceded by a space
*> except when it is the first statement in a compilation group"
*> (cite.py --check: OK §7.2.4.3 2)). REPLACE written directly after a
*> left parenthesis is COBOLNET2449 (kb/Work PB1358). Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1358SP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
           DISPLAY (REPLACE ==XX1== BY ==AAA==.
           DISPLAY XX1.
           STOP RUN.
