*>--- conformance-case
*> id: pb732-value-all-not-symbolic
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: the ALL symbolic-character-1 figurative (ISO 8.3.3.6.2 Format 7), the ONE
*> word-bearing figurative alternative. 8.3.3.6.3 SR4: "Symbolic-character-1 shall be specified in the
*> SYMBOLIC CHARACTERS clause of the SPECIAL-NAMES paragraph." NOSUCHW is not, so the operand names no
*> figurative constant and identifies no resource (8.4.2.1) -> COBOLNET1639. Pre-fix the raw text
*> "ALLNOSUCHW" became the VALUE and the four-character item silently initialized to "ALLN".
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732G.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(4) VALUE ALL NOSUCHW.
PROCEDURE DIVISION.
    DISPLAY A.
    STOP RUN.
