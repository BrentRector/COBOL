*> reject-at: 2002 2014 2023
*> ISO 1989:2023 12.4.5.2 SR7: "Data-name-1 shall reference an alphanumeric data item", and 8.5.2.3
*> is the category alphanumeric. PIC XXXXBXXXX is category ALPHANUMERIC-EDITED (8.5.2.4), a category
*> of its own in 8.5.2.1 Table 2, so it cannot hold the ASSIGN ... USING file name. The screen read
*> the category alone and accepted it (kb/Work PB850).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850AUAE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN USING FILE-NAME-ITEM
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(10).
WORKING-STORAGE SECTION.
01 FILE-NAME-ITEM PIC XXXXBXXXX VALUE "PB85 AUAE".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT SQF
    CLOSE SQF
    STOP RUN.
