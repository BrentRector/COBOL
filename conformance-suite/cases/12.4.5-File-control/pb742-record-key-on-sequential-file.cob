*>--- conformance-case
*> id: pb742-record-key-on-sequential-file
*> rules: [SR-12.4.5.2-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR8 sentence 1 - "Format 1 shall be specified only for an indexed file."
*> The RECORD KEY clause appears ONLY in the 12.4.5.1 Format 1 (indexed) file control entry, so
*> writing it specifies Format 1; this entry's ORGANIZATION clause says SEQUENTIAL, so the file is
*> not an indexed file. Before kb/Work PB742 this compiled with zero diagnostics of any severity:
*> the key name resolved, was stored on the FileModel, and then nothing read it, because every key
*> consumer is on a path a sequential file never enters.
*> Edition-invariant: the RECORD KEY clause and its format rule predate COBOL-85 and no
*> docs/VERSION_CHANGE_REFERENCE.md row touches 12.4.5.2, so all four editions reject.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742SEQRK.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb742seqrk.dat"
        ORGANIZATION IS SEQUENTIAL
        RECORD KEY IS SQ-KEY.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC.
   05 SQ-KEY PIC X(5).
   05 SQ-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    CLOSE SQF
    STOP RUN.
