*> reject-at: 85 2002 2014 2023
*> ISO §6.2.3.2 SR2 "The floating comment indicator of an inline comment
*> shall be preceded by a separator space" (cite.py --check: OK §6.2.3.2
*> 2)). X*> is written with no space: COBOLNET2495 (kb/Work PB1493).
*> Before, the lexer took it as a comment silently. Free form, so the
*> COBOL-85 run is not refused by the fixed-form-only 2002 gate first.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1493NS.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(12).
PROCEDURE DIVISION.
    MOVE "ABC" TO X*> comment
    .
    STOP RUN.
