*>--- conformance-case
*> id: pb699-indexed-no-record-key-entry-only
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.1 Format 1 (indexed): the RECORD KEY clause is written UNBRACKETED in the
*> printed general format, so it is a required member of the indexed file control entry - unlike the
*> RELATIVE KEY clause of Format 2, which is bracketed and required only by 12.4.5.2 SR10. Entry-only
*> (kb/Work PB699): the requirement used to be checked by the first keyed verb naming the file.
IDENTIFICATION DIVISION.
PROGRAM-ID. P699NRK.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p699nrk.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(3).
   05 IX-DATA PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
