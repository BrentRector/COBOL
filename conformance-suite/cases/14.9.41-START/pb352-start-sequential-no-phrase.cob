*>--- conformance-case
*> id: pb352-start-sequential-no-phrase
*> rules: [SR-14.9.41.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR2 - "If the organization of the file referenced by file-name-1 is
*> sequential, either the FIRST or the LAST phrase shall be specified." The general format makes
*> FIRST / KEY / LAST mutually exclusive alternatives of ONE plain bracket (rendered from the printed
*> page - the bracket is optional and at most one alternative is chosen), so on a sequential file the
*> bare START violates SR2 outright. Until kb/Work PB352 this compiled clean and then aborted the run
*> unit with a .NET stack trace, under a message that cited SR2 as the reason for refusing what SR2
*> permits.
IDENTIFICATION DIVISION.
PROGRAM-ID. P352NOPH.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "p352noph.dat"
        ORGANIZATION IS SEQUENTIAL
        ACCESS MODE IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    START SQF END-START
    CLOSE SQF
    STOP RUN.
