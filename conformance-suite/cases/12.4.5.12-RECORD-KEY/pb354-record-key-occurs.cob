*>--- conformance-case
*> id: pb354-record-key-occurs
*> rules: [SR-12.4.5.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.12.3 SR1 - "Data-name-1 and data-name-2 shall not be subject to any OCCURS
*> clauses", the RECORD KEY twin of 12.4.5.13.3 SR1. Swept in with the RELATIVE KEY member: all three
*> key clauses state the same ban and only one of the three had a site.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354KYOC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p354kyoc.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KG.
      10 IX-KEY PIC X(3) OCCURS 2 TIMES.
   05 IX-DATA PIC X(10).
PROCEDURE DIVISION.
MAIN.
    READ IXF NEXT AT END CONTINUE END-READ
    STOP RUN.
