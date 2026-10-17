*>--- conformance-case
*> id: pb348-read-into-strong-group
*> rules: [SR-14.9.30.3-2, GR-14.9.34.4-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.30.4 general rule 4 b) is the SAME SENTENCE as 14.9.34.4 GR5 b): "The current
*> record is moved from the record area to the area specified by identifier-1 according to the rules
*> for the MOVE statement without the CORRESPONDING phrase."  So READ ... INTO a strongly-typed
*> group from an ordinary record area is refused for the same reason its RETURN sibling is
*> (14.9.25.3 SR2 / 8.5.3.3, COBOLNET1533).
*> This fixture exists because the READ arm and the RETURN arm are DIFFERENT CALL SITES -- and the
*> sequential READ and the keyed READ are two more.  kb/Work PB348 binds all four through one
*> MoveBinder.BindIntoPhrase call so they cannot disagree; this is the assertion that they do not.
*> 85 is not listed: TYPEDEF STRONG is a 2002 introduction.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N8.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IN ASSIGN TO "pb348n8.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-IN.
01 F-REC.
   05 F-A PIC X(4).
WORKING-STORAGE SECTION.
01 TA IS TYPEDEF STRONG.
   05 TA-A PIC X(4).
01 WS-RECV TYPE TA.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F-IN
    READ F-IN INTO WS-RECV
        AT END CONTINUE
    END-READ
    CLOSE F-IN
    STOP RUN.
