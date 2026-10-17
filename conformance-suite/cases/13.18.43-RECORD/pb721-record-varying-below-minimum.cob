*>--- conformance-case
*> id: pb721-record-varying-below-minimum
*> rules: [SR-13.18.43.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR4, the FIRST of the rule's two obligations: "Record
*> descriptions for the file shall describe neither records that contain a LESSER
*> number of bytes than that specified by integer-2 nor records that contain a
*> greater number of bytes than that specified by integer-3." F-REC describes 5
*> bytes where the clause states integer-2 = 10. The smallest size a record
*> description describes is 13.18.43.4 GR8 a)'s - every occurs-depending table at
*> its MINIMUM occurrence count - and F-REC has no table, so it is simply 5.
*> The clause itself is well-formed here (SR5 holds: 20 is greater than 10), so
*> this program witnesses SR4's lower arm ALONE. kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR4LO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr4lo.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 10 TO 20 DEPENDING ON WS-LEN.
01 F-REC PIC X(5).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
