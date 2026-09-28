*> reject-at: 85 2002 2014 2023
*> ISO §8.3.5 6) "An opening pseudo-text delimiter shall be immediately
*> preceded by a space; a closing pseudo-text delimiter shall be
*> immediately followed by one of the separators space, comma,
*> semicolon, or period" (cite.py --check: OK §8.3.5 6)). ==XX1==BY
*> glues BY to the closing delimiter: COBOLNET2573 (kb/Work PB1353).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1353DG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "AAA".
       PROCEDURE DIVISION.
           REPLACE ==XX1==BY ==AAA==.
           DISPLAY XX1.
           STOP RUN.
