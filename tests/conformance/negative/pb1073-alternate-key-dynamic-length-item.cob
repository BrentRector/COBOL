*> reject-at: 2014 2023
*> ISO 1989:2023 12.4.5.6.3 SR3 - "Data-name-1 and data-name-2 shall not reference a variable-length
*> data item." The ALTERNATE RECORD KEY twin of 12.4.5.12.3 SR3: IX-ALT is a dynamic-length elementary
*> item (8.5.1.11.1: "a dynamic-capacity table or a dynamic-length elementary item"). It is the LAST
*> item of the record, after the prime key, so the minimum-record-size rule (SR5) has nothing to say
*> and SR3 is the only rule the program breaks (kb/Work PB1073). DYNAMIC LENGTH is a COBOL-2014
*> introduction, so the rule is exercised from 2014 up.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1073ALTDYN.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb1073altdyn.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(5).
   05 IX-DATA PIC X(2).
   05 IX-ALT PIC X DYNAMIC LENGTH.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
