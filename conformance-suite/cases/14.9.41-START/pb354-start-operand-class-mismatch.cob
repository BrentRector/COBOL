*>--- conformance-case
*> id: pb354-start-operand-class-mismatch
*> rules: [SR-14.9.41.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.41.3 SR6 b) 2. - "It has the same class, category, and usage as that record
*> key." IX-NUM is class numeric, category numeric, usage packed-decimal; IX-KEY is class
*> alphanumeric, category alphanumeric, usage display. The condition was omitted by an explicit
*> design note ("a pre-existing looseness kept as-is"), and its absence also made SR6 b) 3.'s length
*> test unsound: it compared a packed item's 4 character positions against an alphanumeric key's
*> byte width, two incommensurable bases. Both halves of the rule land together.
IDENTIFICATION DIVISION.
PROGRAM-ID. P354CLSM.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "p354clsm.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY  PIC X(6).
   05 IX-DATA PIC X(10).
01 IX-REC3.
   05 IX-NUM   PIC 9(4) COMP-3.
   05 IX-NTAIL PIC X(13).
PROCEDURE DIVISION.
MAIN.
    MOVE 1234 TO IX-NUM
    START IXF KEY IS = IX-NUM END-START
    STOP RUN.
