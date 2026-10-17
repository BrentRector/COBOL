*>--- conformance-case
*> id: pb354-start-occurs-operand-relative
*> rules: [SR-14.9.41.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR4 on the RELATIVE arm - the arm that had NO OCCURS test at all. The
*> relative screen compared the operand's DataItem to the RELATIVE KEY item by reference, which is
*> TRUE for a subscripted reference to that same item, and it never called the offset walk that gave
*> the indexed arm its accidental rejection. One rule, two arms, one of them silent.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354OCRL.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT RLF ASSIGN TO "p354ocrl.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS DYNAMIC
        RELATIVE KEY IS WS-RK.
DATA DIVISION.
FILE SECTION.
FD RLF.
01 RL-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-RKT.
   05 WS-RK PIC 9(4) OCCURS 3 TIMES.
PROCEDURE DIVISION.
MAIN.
    START RLF KEY IS = WS-RK (2) END-START
    STOP RUN.
