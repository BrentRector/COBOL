*>--- conformance-case
*> id: pb412-go-to-depending-without-names
*> rules: [FMT-14.9.17.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB412 — A DEPENDING PHRASE WITH NO PROCEDURE-NAME. §14.9.17.2 Format 2 prints
*> `GO TO { procedure-name-1 } … DEPENDING ON identifier-1`; 5.2.6.3 requires one alternative of a brace group
*> to be explicitly specified, so at least one procedure-name is obligatory, and Format 1 has no DEPENDING
*> phrase at all. No format admits this program at any of 1985/2002/2014/2023.
*> WHAT THIS WITNESS PINS: until this fix the program COMPILED CLEAN and the compiler emitted a loud stage, so
*> the run unit died with `NotImplementedCobolFeatureException: … GO TO DEPENDING without procedure-names` —
*> telling the user a FEATURE was unimplemented when the truth was that the source is illegal, and deferring to
*> RUN TIME a defect the compiler had in hand at parse time.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB412NEG2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC 9 VALUE 1.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY "START".
    GO TO DEPENDING ON W-N.
    STOP RUN.
