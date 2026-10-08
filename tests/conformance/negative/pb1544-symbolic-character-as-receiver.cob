*> reject-at: 85 2002 2014 2023
*> A symbolic-character "defines a figurative constant" (ISO 12.3.7.4 GR11 a)), a literal, and a literal is never a
*> receiving operand - the rule COBOLNET1548 states for a constant-name (13.10.4 GR1), asked of the ONE literal-alias
*> resolution (kb/Work PB1544: MOVE ... TO SA was "not defined").
IDENTIFICATION DIVISION.
PROGRAM-ID. NEG1544C.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SA IS 66.
PROCEDURE DIVISION.
MAIN.
    MOVE "B" TO SA.
    STOP RUN.
