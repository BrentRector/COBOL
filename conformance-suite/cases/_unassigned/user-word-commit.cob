*>--- conformance-case
*> id: user-word-commit
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> (kb/Work PB137: the REFERENCE position is now structurally unparseable at 2023 - the reservation-
*> gated cobolWord keeps operand lists from absorbing the word at all - so this fixture pins the
*> DECLARATION screen, where dataName's predicated re-admission lets 0901 NAME the reserved word.)
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGUW01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 COMMIT PIC 9 VALUE 1.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "X".
    STOP RUN.
