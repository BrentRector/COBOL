*>--- conformance-case
*> id: constant-as-receiver
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.10.3 SR2 - a constant-name substitutes a LITERAL, so it shall never be a
*> receiving operand (COBOLNET1548).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC03.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS 42.
PROCEDURE DIVISION.
MAIN.
    MOVE 1 TO K.
    STOP RUN.
