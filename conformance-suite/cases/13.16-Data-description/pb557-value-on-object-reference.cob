*>--- conformance-case
*> id: pb557-value-on-object-reference
*> rules: [SR-13.16.3-10, SR-13.18.63.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR9 - "The VALUE clause shall not be specified if a USAGE clause with a phrase of
*> FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER is also specified." (COBOLNET2168)
*> The screen used to cover TWO of those four usages - the two that already had a diagnostic band - so this
*> program, whose only nonconformance is the VALUE clause, was ACCEPTED.
*> MEASURED BEFORE: rc=0, the program printed OK and the literal was silently discarded. Written with a quoted
*> literal on purpose: `VALUE NULL` on the same entry failed the Roslyn compilation instead (CS0029, 'string'
*> to CobolObject), so a fixture using NULL would have pinned a backend crash and left the SILENT half - the
*> one a user never sees - untested.
*> Rejected from 2002 rather than 85 because USAGE OBJECT REFERENCE is itself post-85; at 85 the entry draws
*> the edition band (COBOLNET0900) instead, which is a different rule.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB557OBJVAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 O USAGE OBJECT REFERENCE VALUE "X".
PROCEDURE DIVISION.
MAIN.
    DISPLAY "OK"
    STOP RUN.
