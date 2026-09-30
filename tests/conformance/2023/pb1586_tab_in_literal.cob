*> options: source-format=free
*> ISO/IEC 1989:2023 6.1 1) c) leaves the meaning of lines and character positions to the
*> implementor; DOC-A.1-157 makes a TAB (U+0009) advance to the next tab stop (positions 1,
*> 9, 17, ...) EVERYWHERE, an alphanumeric literal included (owner decision kb/Work R55):
*> the TAB is an input-medium positioning control, not a character of the literal. A
*> program that wants the character writes X"09" (hexadecimal literal, 8.3.3.2).
*> L: the TAB is at 0-based position 27, so it fills 27..31: the literal is
*>    [A + 5 spaces + B]  (9 characters, exactly PIC X(9)).
*> S: a TAB after the opening apostrophe at position 8 fills 9..15: 7 spaces.
*> F: X"09" is a TAB CHARACTER, not a space: DIFF.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1586LT.
DATA DIVISION.
WORKING-STORAGE SECTION.
    01 L PIC X(9) VALUE "[A	B]".
01 F PIC X VALUE X"09".
PROCEDURE DIVISION.
    DISPLAY L.
DISPLAY '	X'.
    IF F = SPACE DISPLAY "SAME" ELSE DISPLAY "DIFF" END-IF.
    STOP RUN.
