*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.2 SR13 - "Format 4 shall be specified only for a sort-merge file. The
*> associated file description entry shall be a sort-merge file description entry." 12.4.5.1 prints
*> Format 4 (sort-merge) as SELECT [OPTIONAL], ASSIGN and [[ORGANIZATION IS] SEQUENTIAL] and NOTHING
*> else, so the file control entry of a file described by an SD cannot write a FILE STATUS clause:
*> that clause is printed in Formats 1 to 3 only, and each of those closes with "The associated file
*> description entry shall not be a sort-merge file description entry" (SR8, SR9, SR11). FILE STATUS
*> belongs to three formats at once and so names none of them; SR13 is the rule that speaks for it.
*> The same holds for ACCESS MODE, LOCK MODE, RESERVE and SHARING - the clause is checked on the
*> ENTRY, so the SORT statement never has to name the file for the program to be refused (kb/Work PB773).
*> COBOLNET1900: the subject is the file DESCRIPTION entry, not a key clause.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB773SDFS.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRT ASSIGN TO "pb773sdfs.tmp"
        FILE STATUS IS WS-FS.
DATA DIVISION.
FILE SECTION.
SD SRT.
01 SR-REC.
   05 SR-KEY PIC X(5).
   05 SR-DATA PIC X(5).
WORKING-STORAGE SECTION.
01 WS-FS PIC XX.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
