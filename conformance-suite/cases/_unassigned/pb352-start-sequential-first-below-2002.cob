*>--- conformance-case
*> id: pb352-start-sequential-first-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41 - START FIRST/LAST is a COBOL-2002 introduction, and the gate fires on the
*> BOUND NODE's Mode. Before kb/Work PB352 the sequential-organization arm returned BoundUnsupported
*> BEFORE any BoundKeyedStart existed, so this program compiled at --std 85 with no edition
*> diagnostic at all: the construct reached no gate because it reached no bound node. It now binds
*> like its relative and indexed twins and the introduction gate sees it.
IDENTIFICATION DIVISION.
PROGRAM-ID. P352G85.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "p352g85.dat"
        ORGANIZATION IS SEQUENTIAL
        ACCESS MODE IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    START SQF FIRST END-START
    CLOSE SQF
    STOP RUN.
