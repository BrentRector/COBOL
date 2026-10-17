*>--- conformance-case
*> id: pb721-record-varying-inverted-range
*> rules: [SR-13.18.43.3-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR5: "Integer-3 shall be greater than integer-2." The clause
*> states FROM 20 TO 5.
*> !! THE RULE IS ABOUT THE CLAUSE, AND THE COMPILER MUST SAY SO. Before kb/Work
*> PB721 this compiled clean at every edition and the program learned about it only
*> at run time, as an I-O status '44' on the first WRITE - which is 13.18.43.4
*> GR14 a)'s CONSEQUENCE ("If the number of bytes in the record to be written is
*> less than integer-2 or greater than integer-3 ... the EC-I-O-LOGIC-ERROR
*> exception condition is set to exist"), never the clause's diagnosis. An inverted
*> range makes EVERY write unsuccessful, so the run-time report is also unusably
*> late.
*> A record description cannot satisfy an inverted range either (no size is both at
*> least 20 and at most 5), so the SR4 diagnostic accompanies this one by necessity;
*> the .err names SR5's own sentence, which is the fact under test. kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr5.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 20 TO 5 DEPENDING ON WS-LEN.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
