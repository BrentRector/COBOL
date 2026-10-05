*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.6.3 SR4 - "Data-name-1 shall not reference an item whose leftmost byte
*> position corresponds to the leftmost byte position of the prime record key, or of another
*> alternate record key. This restriction does not apply in the case where either key is specified
*> using the SOURCE phrase." Neither key here uses SOURCE. The alternate key IX-G1 is the FIRST
*> subordinate of the group IX-KEY, which is the prime key, so both begin at byte 1 of the record
*> although they are different data items of different lengths: the rule compares POSITIONS, not
*> names (kb/Work PB1073). A second ALTERNATE RECORD KEY at byte 5 is legal and must not be reported,
*> which is why the diagnostic is expected exactly once and names IX-G1.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1073ALTPRIME.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb1073altprime.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-G1 WITH DUPLICATES
        ALTERNATE RECORD KEY IS IX-A2 WITH DUPLICATES.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY.
      10 IX-G1 PIC X(2).
      10 IX-G2 PIC X(2).
   05 IX-A2 PIC X(2).
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
