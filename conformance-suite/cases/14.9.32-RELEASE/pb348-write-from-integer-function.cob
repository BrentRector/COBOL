*>--- conformance-case
*> id: pb348-write-from-integer-function
*> rules: [SR-14.9.32.3-2, SR-14.9.51.3-4, SR-14.9.51.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.51.3 syntax rule 4: "If identifier-1 is a function-identifier, it shall
*> reference an alphanumeric or national function."  WRITE states the rule UNCONDITIONALLY -- it is
*> not the FILE-phrase reading (SR10) nor the no-FILE reading (SR11, which admits a boolean function
*> too), so a conforming WRITE ... FROM function-identifier satisfies SR4 and SR11 only inside SR4's
*> narrower set.  FUNCTION LENGTH is an integer function and is outside it.
*> The SIBLING half of kb/Work PB348: WRITE, REWRITE and RELEASE shared ONE operand binder that
*> applied none of the three rules, so fixing RELEASE alone would have left this arm silent
*> (feedback_two_arm_dispatch).  The paired positive is the alphanumeric-function admission pinned
*> by conformance:cases/8.4.3.1-Identifier/pb10_function_identifier_sending.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB348N2.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-OUT ASSIGN TO "pb348n2.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD F-OUT.
01 F-REC PIC X(8).
WORKING-STORAGE SECTION.
01 WS-A PIC X(8) VALUE "ABCDEFGH".
PROCEDURE DIVISION.
MAIN.
    OPEN OUTPUT F-OUT
    WRITE F-REC FROM FUNCTION LENGTH(WS-A)
    CLOSE F-OUT
    STOP RUN.
