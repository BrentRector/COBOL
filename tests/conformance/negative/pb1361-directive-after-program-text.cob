*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.3 SR2 "A compiler directive shall be preceded only by zero, one, or more space
*> characters." (kb/Work PB1361) In free form the whole line is the program-text area, so "000100 >>SOURCE"
*> is not a SOURCE FORMAT directive: the sequence number is program text and the line is rejected.
>>SOURCE FORMAT FREE
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1361-NEG-DIGITS-BEFORE.
PROCEDURE DIVISION.
    DISPLAY "FREE".
000100 >>SOURCE FORMAT FIXED
    STOP RUN.
