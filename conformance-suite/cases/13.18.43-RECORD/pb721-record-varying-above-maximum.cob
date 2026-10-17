*>--- conformance-case
*> id: pb721-record-varying-above-maximum
*> rules: [SR-13.18.43.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR4, the SECOND of the rule's two obligations: "... NOR records
*> that contain a greater number of bytes than that specified by integer-3."
*> F-REC describes 20 bytes where the clause states integer-3 = 5. The largest size
*> a record description describes is 13.18.43.4 GR8 b)'s - every occurs-depending
*> table at its MAXIMUM occurrence count.
*> The clause itself is well-formed (SR5 holds: 5 is greater than 1) and the lower
*> arm is satisfied (20 is not fewer than 1), so this program witnesses SR4's upper
*> arm ALONE - the pair with pb721-record-varying-below-minimum is what proves the
*> single printed sentence got TWO screens and not one. kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR4HI.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr4hi.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 1 TO 5 DEPENDING ON WS-LEN.
01 F-REC PIC X(20).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
