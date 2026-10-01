*> reject-at: 2002 2014 2023
*> 6.4.2: 'At least one alphanumeric character, national character, or
*> hexadecimal digit of the literal content shall be specified on the
*> continued line and on each continuation line.' The continuation line
*> below holds none (kb/Work PB1359, free form).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1359EMP.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(12) VALUE "AB"-
   "".
PROCEDURE DIVISION.
DISPLAY X.
    STOP RUN.
