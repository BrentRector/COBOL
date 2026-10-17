*>--- conformance-case
*> id: pb742-relative-format-on-sort-merge-entry
*> rules: [SR-12.4.5.2-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 SR9 sentence 2, and the case NO KEY-CLAUSE row could ever reach: this
*> entry writes no key clause at all. It specifies 12.4.5.1's Format 2 solely by its ORGANIZATION
*> clause - "[ ORGANIZATION IS ] RELATIVE" appears in Format 2 and nowhere else - while an SD
*> describes the file. That is why the screen has a rule stated about the ENTRY rather than about
*> one operand of it (kb/Work PB742); a per-clause screen would have shipped this silently.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB742SDREL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SRT ASSIGN TO "pb742sdrel.tmp"
        ORGANIZATION IS RELATIVE.
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
