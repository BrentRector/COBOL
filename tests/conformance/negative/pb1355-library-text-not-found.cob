*> reject-at: 85 2002 2014 2023
*> ISO §7.2.3.4 GR1 "Text-name-1 or literal-1 identifies the library
*> text to be processed by the COPY statement" and GR2 "Library-name-1
*> names a resource that shall be available to the compiler and shall
*> provide access to the library text" (cite.py --check: OK §7.2.3.4 1)
*> and 2)). No library text PB1355NONE exists anywhere the default COBOL
*> library looks (DOC-A.1-40), so the COPY is CBL3620 at every edition
*> (kb/Work PB1355). Before, the COPY became a comment with no
*> diagnostic and the program compiled without the copied text.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1355NF.
DATA DIVISION.
WORKING-STORAGE SECTION.
COPY PB1355NONE.
01 W PIC X(2) VALUE "OK".
PROCEDURE DIVISION.
    DISPLAY W
    STOP RUN.
