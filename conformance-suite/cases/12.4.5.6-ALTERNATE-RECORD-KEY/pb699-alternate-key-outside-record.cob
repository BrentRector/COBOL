*>--- conformance-case
*> id: pb699-alternate-key-outside-record
*> rules: [SR-12.4.5.6.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.6.3 SR2 - "Data-name-1 and data-name-2 shall be defined as a data item of
*> category alphanumeric or national within a record description entry associated with the file-name
*> to which the ALTERNATE RECORD KEY clause is subordinate." An unresolvable or out-of-record
*> alternate key used to be DROPPED SILENTLY from the resolved key list, so the file was built with
*> fewer access paths than the entry declared and no diagnostic said so (kb/Work PB699).
IDENTIFICATION DIVISION.
PROGRAM-ID. P699AKX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p699akx.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS WS-OUTSIDE.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(3).
   05 IX-DATA PIC X(10).
WORKING-STORAGE SECTION.
01 WS-OUTSIDE PIC X(4).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
