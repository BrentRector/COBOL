*>--- conformance-case
*> id: pb721-record-contains-larger-record
*> rules: [SR-13.18.43.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR3, the FORMAT 1 (fixed-length) rule: "No record description
*> entry for the file may specify a number of bytes greater than integer-1."
*> F-REC describes 20 bytes and the clause states integer-1 = 10, so the entry is
*> not conforming source. The size compared is 13.18.43.4 GR3's - "the sum of the
*> number of bytes in all fixed length elementary items plus the sum of the maximum
*> number of bytes in any occurs-depending table subordinate to the record".
*> Until kb/Work PB721 nothing screened this rule and the program compiled clean.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr3.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 10.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
