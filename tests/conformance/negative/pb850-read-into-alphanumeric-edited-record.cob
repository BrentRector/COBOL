*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 14.9.30.3 SR1 admits READ ... INTO when "a) ... only one record description is
*> subordinate to the file description entry" or "b) ... the data item referenced by identifier-1
*> and all record-names associated with file-name-1 describe an alphanumeric group item or an
*> elementary item of category alphanumeric or category national". This FD has TWO records and
*> R2 (PIC XX/XX) is category ALPHANUMERIC-EDITED (8.5.2.4), so neither ground holds. The screen
*> read the category alone and accepted the INTO phrase (kb/Work PB850).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850RIAE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb850riae.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 R1 PIC X(5).
01 R2 PIC XX/XX.
WORKING-STORAGE SECTION.
01 W PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    READ SQF INTO W AT END CONTINUE END-READ
    CLOSE SQF
    STOP RUN.
