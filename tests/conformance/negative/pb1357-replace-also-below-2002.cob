*> reject-at: 85
*> ISO §7.2.4.2 format 1 REPLACE [ALSO] and format 2 REPLACE [LAST] OFF
*> (cite.py --check 7.2.4.2 "REPLACE [ ALSO ]": OK). The stacking phrases
*> are a COBOL-2002 introduction (constructs row replace-also-last-2002,
*> a provisional pre-2023 edge): COBOLNET0900 at COBOL-85 (kb/Work
*> PB1357). Fixed form, columns 8-72.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1357AL85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AA PIC X(2) VALUE "AA".
       PROCEDURE DIVISION.
           REPLACE ALSO ==XX1== BY ==AA==.
           DISPLAY XX1.
           REPLACE LAST OFF.
           STOP RUN.
