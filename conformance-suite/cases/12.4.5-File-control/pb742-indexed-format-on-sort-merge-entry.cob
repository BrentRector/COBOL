*>--- conformance-case
*> id: pb742-indexed-format-on-sort-merge-entry
*> rules: [SR-12.4.5.2-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR8 sentence 2 - "The associated file description entry shall not be a
*> sort-merge file description entry." The SECOND obligation of SR8, and the reason the rule is two
*> rows rather than one: sentence 1 speaks about the file's ORGANIZATION, and a sort-merge file has
*> none to speak of (12.4.5.1 Format 4 admits only the SEQUENTIAL phrase and no key clause at all).
*> Here the entry specifies Format 1 by writing a RECORD KEY clause while an SD describes the file.
*> COBOLNET1900, not COBOLNET0863: the subject is the file DESCRIPTION entry, not a key clause's own
*> rule, and the remedy is a different edit (describe the file with an FD, or drop the clause).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742SDIX.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRT ASSIGN TO "pb742sdix.tmp"
        RECORD KEY IS SR-KEY.
DATA DIVISION.
FILE SECTION.
SD SRT.
01 SR-REC.
   05 SR-KEY PIC X(5).
   05 SR-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    DISPLAY "UNREACHED"
    STOP RUN.
