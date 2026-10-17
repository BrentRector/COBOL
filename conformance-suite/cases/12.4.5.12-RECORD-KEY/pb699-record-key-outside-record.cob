*>--- conformance-case
*> id: pb699-record-key-outside-record
*> rules: [SR-12.4.5.12.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.12.3 SR2 - "Data-name-1 and data-name-2 shall reference a data item of
*> category alphanumeric or category national within a record description entry associated with the
*> file-name specified in this file control entry." data-name-1 here is a WORKING-STORAGE item, so
*> the record has no key positions at all. Before kb/Work PB699 this compiled clean and the alternate
*> access path bound to storage outside the record area. (The screen implements the WITHIN-A-RECORD
*> half of SR2 for data-name-1; the category half and data-name-2 are recorded PARTIAL.)
IDENTIFICATION DIVISION.
PROGRAM-ID. P699RKX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p699rkx.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS WS-OUTSIDE.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(3).
   05 IX-DATA PIC X(10).
WORKING-STORAGE SECTION.
01 WS-OUTSIDE PIC X(3).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
