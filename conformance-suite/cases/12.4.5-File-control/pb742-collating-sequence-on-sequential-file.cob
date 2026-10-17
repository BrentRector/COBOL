*>--- conformance-case
*> id: pb742-collating-sequence-on-sequential-file
*> rules: [SR-12.4.5.2-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR8 sentence 1 over its THIRD operand - the collating-sequence-clause,
*> which 12.4.5.1 prints in Format 1 and in no other format. The rejection itself is not new
*> (COBOLNET1582 has always refused it); its CITATION was 12.4.5.7.1, the clause's descriptive
*> General paragraph, which states no obligation. SR8 is the sentence that does, and the message now
*> says so (kb/Work PB742, CLAUDE.md rule 1: a citation is validated, never inherited).
*> The traceability row SR-12.4.5.2-8 therefore names TWO code locations: the key-clause rows in
*> FileControlKeyRules, and this site - which reports where it does because the same test is also the
*> guard that stops the rest of the collating resolution.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742SEQCOL.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET ALPHA-X IS NATIVE.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb742seqcol.dat"
        ORGANIZATION IS SEQUENTIAL
        COLLATING SEQUENCE IS ALPHA-X.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC PIC X(10).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    CLOSE SQF
    STOP RUN.
