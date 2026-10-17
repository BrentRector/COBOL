*>--- conformance-case
*> id: user-word-validate
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> VALIDATE (the STATEMENT is §14.9.50; this fixture is about the WORD) is a USER WORD at COBOL-85 and
*> reserved from 2002 onward (§8.9). Wave H's hard lexer token must
*> not change that: the cobolWord nameSlot funnel keeps `01 VALIDATE` legal at --std 85.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGUWVAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 VALIDATE PIC X(4) VALUE "ABCD".
PROCEDURE DIVISION.
MAIN.
    DISPLAY VALIDATE.
    STOP RUN.
