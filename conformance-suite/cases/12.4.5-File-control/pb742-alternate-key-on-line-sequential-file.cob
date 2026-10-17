*>--- conformance-case
*> id: pb742-alternate-key-on-line-sequential-file
*> rules: [SR-12.4.5.2-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR8 sentence 1, over the OTHER Format-1 key clause and over the OTHER
*> sequential phrase. The ALTERNATE RECORD KEY clause is Format 1's alone (12.4.5.1), and LINE
*> SEQUENTIAL is a sequential organization (12.4.5.10.3 GR3), not an indexed one. The alternate
*> clause is screened per CLAUSE AS WRITTEN, so the rule fires on a clause whose data-name resolves
*> perfectly well - the violation is the clause's presence, not its operand.
*> ⚠ LINE SEQUENTIAL is a COBOL-2023 introduction as an ORGANIZATION phrase in this compiler, but
*> the file control entry is still rejected at every edition here because SR8 is what refuses it and
*> the below-2023 editions add their own introduction gate on top; the reject-at header names all
*> four because the case must be REFUSED at all four, whichever rule speaks first.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742LSALT.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb742lsalt.dat"
        ORGANIZATION IS LINE SEQUENTIAL
        ALTERNATE RECORD KEY IS SQ-ALT.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 SQ-REC.
   05 SQ-ALT PIC X(5).
   05 SQ-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    CLOSE SQF
    STOP RUN.
