*> reject-at: 85 2002 2014 2023
*> ISO §7.2.4.4 GR9 "The text produced as a result of processing a
*> REPLACE statement shall not contain a COPY statement, a REPLACE
*> statement, a SOURCE FORMAT directive, a comment, or a blank line"
*> (cite.py --check: OK §7.2.4.4 9)). XX1 is replaced by the word
*> REPLACE: COBOLNET2574 (kb/Work PB1356). Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1356RR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
           REPLACE ==XX1== BY ==REPLACE==.
           XX1 ==AAA== BY ==BBB==.
           DISPLAY AAA.
           STOP RUN.
