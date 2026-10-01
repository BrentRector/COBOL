*> reject-at: 2014 2023
*> kb/Work PB1562 (the lower arm) - ISO 1989 §13.18.43.3 SR4: record descriptions "shall describe neither records that
*> contain a lesser number of bytes than that specified by integer-2". A record description's smallest size is GR8 a)'s
*> sum with every variable member at its minimum, and a dynamic-length item may be zero length (§8.5.4 item 4: "A
*> data item defined with the DYNAMIC LENGTH clause that is a zero-length item" - a data item "whose minimum length
*> is zero") - so the record below can contain 3 bytes, fewer than integer-2 = 5. The screen used to skip every
*> record holding a dynamic-length item.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1562LO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb1562lo.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 5 TO 50 CHARACTERS.
01 R.
   05 KY PIC X(3).
   05 C  PIC X DYNAMIC LENGTH LIMIT 20.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
