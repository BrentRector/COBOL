*>--- conformance-case
*> id: pb421-move-literal-then-corr
*> rules: [FMT-14.9.25.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB421 — THE OTHER TWO ARMS OF THE SAME OVER-ACCEPTANCE, so the repair cannot be half applied
*> (feedback_two_arm_dispatch). The deleted `moveReceivingPhrase` alternative spelled the keyword
*> `(CORRESPONDING | CORR)` and the statement's sending position admits a LITERAL as well as an identifier, so
*> the shape had four spellings and a fix that reached only the identifier/CORRESPONDING pair would leave this
*> one compiling. §14.9.25.2's two general formats admit neither: Format 2's keyword is written immediately
*> after MOVE, and §14.9.25.3 rule 11 makes CORR and CORRESPONDING equivalent — equivalent in the position the
*> format prints them, not in a position it does not.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB421NEG2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G1.
   05 F1 PIC X(3) VALUE "XYZ".
01 G2.
   05 F1 PIC X(3).
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE "Q" CORR G1 TO G2.
    DISPLAY "REACHED".
    STOP RUN.
