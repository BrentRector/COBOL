*> reject-at: 85 2002 2014 2023
*> ISO §7.2.3.4 GR2 "Library-name-1 names a resource that shall be
*> available to the compiler and shall provide access to the library
*> text" (cite.py --check: OK §7.2.3.4 2)). No library PB1355NOLIB is
*> available, so the COPY is CBL3620 (kb/Work PB1355) - the text is
*> NOT taken from the default library instead (DOC-A.1-40: no fallback).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1355NL.
DATA DIVISION.
WORKING-STORAGE SECTION.
COPY PB1355NONE OF PB1355NOLIB.
01 W PIC X(2) VALUE "OK".
PROCEDURE DIVISION.
    DISPLAY W
    STOP RUN.
