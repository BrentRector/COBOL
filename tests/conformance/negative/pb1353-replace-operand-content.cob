*> reject-at: 85 2002 2014 2023
*> ISO §7.2.4.3 SR3 "Pseudo-text-1 shall contain one or more text-words,
*> at least one of which shall be neither a separator comma nor a
*> separator semicolon", SR5 "Partial-word-1 shall consist of one
*> text-word" (cite.py --check: OK §7.2.4.3 3) and 5)). ==,== names no
*> text-word: COBOLNET2572 (kb/Work PB1353) - before, the operand was
*> silently discarded. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1353RC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
           REPLACE ==,== BY ==AAA==.
           DISPLAY AAA.
           STOP RUN.
