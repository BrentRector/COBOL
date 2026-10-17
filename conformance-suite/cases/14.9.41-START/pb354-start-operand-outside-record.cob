*>--- conformance-case
*> id: pb354-start-operand-outside-record
*> rules: [SR-14.9.41.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR6 b) 1. - the operand's leftmost character position shall correspond to
*> a record key's leftmost position "WITHIN A RECORD OF THE FILE". Nothing required that: the offset
*> walk measured every operand inside its OWN topmost 01, so offset 0 in ANY 01 anywhere - a
*> WORKING-STORAGE group included - collided with offset 0 in the record and bound as a generic key.
*> WS-PART is not in a record of IXF and shall be rejected.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354WSOP.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p354wsop.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY  PIC X(6).
   05 IX-DATA PIC X(10).
WORKING-STORAGE SECTION.
01 WS-OUTSIDE.
   05 WS-PART   PIC X(4).
   05 WS-FILLER PIC X(20).
PROCEDURE DIVISION.
MAIN.
    START IXF KEY IS = WS-PART END-START
    STOP RUN.
