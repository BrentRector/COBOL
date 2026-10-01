*> reject-at: 2014 2023
*> kb/Work PB1562 (the sibling arm) - ISO 1989 §13.18.43.3 SR4: record descriptions "shall describe neither records
*> that contain a lesser number of bytes than that specified by integer-2 nor records that contain a greater number of
*> bytes than that specified by integer-3". The upper arm compares GR8 b)'s maximum, which for a dynamic-length item
*> is its LIMIT or, with none, the implementor maximum (§8.5.1.10.1; DOC-A.1-62) - so the unbounded member below
*> describes records far above integer-3 = 100. The screen used to skip every record holding a dynamic-length item,
*> for this Format 2 clause as for Format 1's SR3 (pb1562-record-contains-unbounded-dynamic).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1562UV.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb1562uv.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD IS VARYING IN SIZE FROM 3 TO 100 CHARACTERS.
01 R.
   05 KY PIC X(3).
   05 C  PIC X DYNAMIC LENGTH.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
