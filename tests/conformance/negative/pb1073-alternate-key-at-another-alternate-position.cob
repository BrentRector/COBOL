*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.6.3 SR4 - "Data-name-1 shall not reference an item whose leftmost byte
*> position corresponds to the leftmost byte position of the prime record key, or of another
*> alternate record key." The SECOND alternate key (IX-G1) begins where the FIRST one (IX-GRP, a
*> group whose first subordinate it is) begins, byte 5 of the record, while the prime key at byte 1
*> is not involved. The rule is stated about every alternate key and counts every OTHER alternate
*> key as the one it may not coincide with, so BOTH clauses are in violation and each names the
*> other (kb/Work PB1073).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1073ALTALT.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb1073altalt.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-GRP WITH DUPLICATES
        ALTERNATE RECORD KEY IS IX-G1 WITH DUPLICATES.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(4).
   05 IX-GRP.
      10 IX-G1 PIC X(2).
      10 IX-G2 PIC X(2).
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
