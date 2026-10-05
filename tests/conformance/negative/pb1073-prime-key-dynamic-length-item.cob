*> reject-at: 2014 2023
*> ISO 1989:2023 12.4.5.12.3 SR3 - "Data-name-1 and data-name-2 shall not reference a variable-length
*> data item." 8.5.1.11.1 defines the term: "a dynamic-capacity table or a dynamic-length elementary
*> item", and IX-KEY below is the second kind (DYNAMIC LENGTH, 8.5.1.10). The record key of an indexed
*> file is located by BYTE POSITION and compared byte for byte, and a dynamic-length item has neither a
*> fixed position nor a fixed length. The table arm is a key under OCCURS, which SR1 already reports
*> (kb/Work PB1073), so this program is the elementary arm. DYNAMIC LENGTH is a COBOL-2014
*> introduction, so the rule is exercised from 2014 up.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1073PRIMEDYN.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb1073primedyn.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X DYNAMIC LENGTH.
   05 IX-DATA PIC X(2).
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
