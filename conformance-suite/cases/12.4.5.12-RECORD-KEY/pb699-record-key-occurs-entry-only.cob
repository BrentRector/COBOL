*>--- conformance-case
*> id: pb699-record-key-occurs-entry-only
*> rules: [SR-12.4.5.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.12.3 SR1 - "Data-name-1 and data-name-2 shall not be subject to any OCCURS
*> clauses." THE DISCRIMINATOR against pb354-record-key-occurs: that program contains a READ, and the
*> rule used to be screened by the first KEYED VERB naming the file. This one only OPENs and CLOSEs,
*> so before kb/Work PB699 it compiled with zero diagnostics. The rule is a syntax rule of the FILE
*> CONTROL ENTRY: the entry violates it whether or not a statement references the file.
IDENTIFICATION DIVISION.
PROGRAM-ID. P699RKO.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p699rko.dat"
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
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
