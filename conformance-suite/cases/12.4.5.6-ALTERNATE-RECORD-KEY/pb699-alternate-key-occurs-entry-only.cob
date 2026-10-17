*>--- conformance-case
*> id: pb699-alternate-key-occurs-entry-only
*> rules: [SR-12.4.5.6.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.6.3 SR1 - "Data-name-1 and data-name-2 shall not be subject to any OCCURS
*> clauses", the ALTERNATE RECORD KEY twin. Entry-only: OPEN and CLOSE are the whole procedure
*> division, so no keyed verb ever names the file (kb/Work PB699). The prime key here is LEGAL, which
*> is what makes this the alternate clause's own witness rather than a second copy of the prime one.
IDENTIFICATION DIVISION.
PROGRAM-ID. P699AKO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p699ako.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-ALT.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(3).
   05 IX-AG.
      10 IX-ALT PIC X(4) OCCURS 2 TIMES.
   05 IX-DATA PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
