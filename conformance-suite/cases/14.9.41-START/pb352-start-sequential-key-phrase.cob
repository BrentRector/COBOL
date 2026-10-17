*>--- conformance-case
*> id: pb352-start-sequential-key-phrase
*> rules: [SR-14.9.41.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR2 - the KEY-phrase arm of the same rule. SR2 requires FIRST or LAST on a
*> sequential-organization file, and the general format admits at most ONE of FIRST / KEY / LAST, so a
*> KEY phrase here is the same violation as the bare form and shall be reported the same way. The
*> pairing matters: SR2's two spellings used to share ONE early return that reported neither.
IDENTIFICATION DIVISION.
PROGRAM-ID. P352KYPH.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "p352kyph.dat"
        ORGANIZATION IS SEQUENTIAL
        ACCESS MODE IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    START SQF KEY IS = SQ-REC END-START
    CLOSE SQF
    STOP RUN.
