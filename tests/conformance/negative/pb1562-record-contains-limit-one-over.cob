*> reject-at: 2014 2023
*> kb/Work PB1562 - the BOUNDARY of ISO 1989 §13.18.43.3 SR3 ("No record description entry for the file may specify a
*> number of bytes greater than integer-1"): the dynamic members' LIMITs plus the fixed member make 10 + 3 + 8 = 21 bytes,
*> ONE over integer-1 = 20, so the entry is refused; the same entry with LIMIT 9 on the first member (exactly 20) is the
*> positive golden tests/conformance/2014/pb1562_variable_group_under_fixed_record_clause.cob. A dynamic-length item counts at
*> its maximum size, the smallest of its LIMIT, its prefixed usage's largest integer and the implementor maximum (§8.5.1.10.1).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1562L1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F ASSIGN TO "pb1562l1.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F RECORD CONTAINS 20 CHARACTERS.
01 R.
   05 A  PIC X DYNAMIC LENGTH LIMIT 10.
   05 KY PIC X(3).
   05 C  PIC X DYNAMIC LENGTH LIMIT 8.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F
    CLOSE F
    STOP RUN.
