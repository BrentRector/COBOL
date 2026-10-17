*>--- conformance-case
*> id: pb721-record-contains-negative-minimum
*> rules: [SR-13.18.43.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR8: "Integer-4 shall be greater than or equal to zero" - SR7's
*> twin over the Format 3 clause, and the same two halves. The permitting half is
*> the zero-length-record goldens; this is the rejecting one.
*> Same mechanism, same citation: 5.5 rule 1 makes integer-4 an unsigned literal, so
*> RECORD CONTAINS -1 TO 20 is refused where the format writes it. The pair exists
*> because a screen written for Format 2 alone is the shape that ships one arm of a
*> two-format rule (the repository's most reproducible defect). kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR8NEG.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr8neg.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS -1 TO 20.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
