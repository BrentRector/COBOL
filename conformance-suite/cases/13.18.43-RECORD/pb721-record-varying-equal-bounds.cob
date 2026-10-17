*>--- conformance-case
*> id: pb721-record-varying-equal-bounds
*> rules: [SR-13.18.43.3-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR5 again, on the boundary that a ">=" reading would wave
*> through: "Integer-3 shall be greater than integer-2" - GREATER, not "greater than
*> or equal to", and the standard writes the weaker relation explicitly where it
*> means it (SR7 two rules later: "Integer-2 shall be greater than or EQUAL TO
*> zero"). FROM 5 TO 5 is therefore not conforming source, and a fixed-size file is
*> written in the Format 1 clause instead.
*> This case exists because the equal-bounds arm is exactly where an off-by-one
*> screen passes its own inverted-range test and still ships wrong. kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR5EQ.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr5eq.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 5 TO 5 DEPENDING ON WS-LEN.
01 F-REC PIC X(5).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
