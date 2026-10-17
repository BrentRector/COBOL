*>--- conformance-case
*> id: pb354-alternate-key-occurs
*> rules: [SR-12.4.5.6.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.6.3 SR1 - "Data-name-1 and data-name-2 shall not be subject to any OCCURS
*> clauses", the ALTERNATE RECORD KEY twin. The third member of the same set.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354ALOC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p354aloc.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-ALT.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(6).
   05 IX-AG.
      10 IX-ALT PIC X(4) OCCURS 2 TIMES.
   05 IX-DATA PIC X(6).
PROCEDURE DIVISION.
MAIN.
    READ IXF NEXT AT END CONTINUE END-READ
    STOP RUN.
