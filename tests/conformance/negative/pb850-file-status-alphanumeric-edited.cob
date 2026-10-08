*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.8.3 SR2: "Data-name-1 shall reference a two-character data item of the
*> category alphanumeric". PIC X0 is two characters long but category ALPHANUMERIC-EDITED (8.5.2.4),
*> a category of its own in 8.5.2.1 Table 2, so SR2 refuses it. It compiled and ran with FS=00
*> before kb/Work PB850; GnuCOBOL refuses it too.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850FSAE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb850fsae.dat"
        ORGANIZATION IS SEQUENTIAL
        FILE STATUS IS SQ-STATUS.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(10).
WORKING-STORAGE SECTION.
01 SQ-STATUS PIC X0.
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT SQF
    CLOSE SQF
    STOP RUN.
