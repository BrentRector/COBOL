*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 14.9.41.3 SR6 b) 2. - "It has the same class, category, and usage as that record
*> key." IX-ED (PIC XXBXX) begins at the key's leftmost character position and is no longer than it,
*> and it is class alphanumeric and usage display like IX-KEY (PIC X(6)), but its category is
*> ALPHANUMERIC-EDITED (8.5.2.4), not alphanumeric: 8.5.2.1 Table 2 lists the two separately. The
*> comparison read the model's category alone, which carries both as PicCategory.Alphanumeric, and
*> accepted the operand (kb/Work PB850's sibling sweep).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850STAE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb850stae.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY  PIC X(6).
   05 IX-DATA PIC X(10).
01 IX-REC2.
   05 IX-ED   PIC XXBXX.
   05 IX-TAIL PIC X(11).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    MOVE "ABCD" TO IX-ED
    START IXF KEY IS = IX-ED END-START
    CLOSE IXF
    STOP RUN.
