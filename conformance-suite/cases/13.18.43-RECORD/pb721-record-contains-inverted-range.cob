*>--- conformance-case
*> id: pb721-record-contains-inverted-range
*> rules: [SR-13.18.43.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.43.3 SR9, SR5's twin one general format over: "Integer-5 shall be
*> greater than integer-4", of the Format 3 clause RECORD CONTAINS integer-4 TO
*> integer-5. The clause states 20 TO 5.
*> !! FORMAT 3 HAS NO SR4-ANALOGUE, and that is why this case is here as well as the
*> Format 2 one: 13.18.43.4 GR18 says that for this format "the size of each record
*> is completely defined in the record description entry", so the standard states no
*> rule bounding the record descriptions by integer-4 and integer-5, and SR9 is the
*> ONLY thing that can catch an inverted Format 3 range. F-REC's 10 bytes draw
*> nothing, deliberately - a screen that also reported the record here would be
*> enforcing a rule the standard does not state. kb/Work PB721.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB721SR9.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb721sr9.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 20 TO 5.
01 F-REC PIC X(10).
WORKING-STORAGE SECTION.
01 WS-LEN PIC 9(4) VALUE 0.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
