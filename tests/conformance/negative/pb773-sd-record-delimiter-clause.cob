*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 12.4.5.2 SR11 sentence 2 - "The associated file description entry shall not be a
*> sort-merge file description entry." The RECORD DELIMITER clause is printed in 12.4.5.1 Format 3
*> (sequential) ALONE, so writing it specifies Format 3, and an SD describes the file. Sentence 1 of
*> the rule is about the file's organization and a sort-merge file has none; this second sentence is
*> the one that speaks about it, which is why the rule is two rows (kb/Work PB773). The clause is
*> otherwise a declined accept-inert clause that draws only a warning, so the refusal below is the
*> program's only error.
*> COBOLNET1900, not COBOLNET0863: the subject is the file DESCRIPTION entry.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB773SDRD.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRT ASSIGN TO "pb773sdrd.tmp"
        RECORD DELIMITER IS STANDARD-1.
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
