*> reject-at: 2014 2023
*> kb/Work PB1562 - ISO 1989 §13.18.43.3 SR3: "No record description entry for the file may specify a number of
*> bytes greater than integer-1." The maximum number of bytes a record description specifies is GR8 b)'s (§13.18.43.4),
*> and a dynamic-length item's maximum is the smallest of its LIMIT phrase, the largest integer its prefixed usage can
*> store, and the implementor maximum (§8.5.1.10.1; DOC-A.1-62: 1,073,741,791 characters). Neither dynamic member
*> here carries a LIMIT, so the record may describe more than a billion bytes and RECORD CONTAINS 20 cannot hold it.
*> The screen used to skip every record holding a dynamic-length item, so this entry compiled clean and its records
*> read back as one member holding the whole block. The DYNAMIC LENGTH clause is COBOL-2014 (§8.5.1.10), so the
*> rule has nothing to say below it; the same entry with LIMIT phrases that fit is the positive golden
*> tests/conformance/2014/pb1562_variable_group_under_fixed_record_clause.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1562UN.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb1562un.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 20 CHARACTERS.
01 R.
   05 A  PIC X DYNAMIC LENGTH.
   05 KY PIC X(3).
   05 C  PIC X DYNAMIC LENGTH.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
