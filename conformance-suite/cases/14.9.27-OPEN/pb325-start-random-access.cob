*>--- conformance-case
*> id: pb325-start-random-access
*> rules: [GR-14.9.27.4-8, SR-14.9.41.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB325 - ISO 14.9.27.4 GR8, Table 20 "Permissible I-O statements by access mode and open mode":
*> the Random row's START cells are BLANK in every one of the four open-mode columns, so no open mode makes a
*> START legal on an ACCESS RANDOM connector. The blank row is not a run-time status - unlike WRITE's item-8
*> '48' and REWRITE/DELETE's item-9 '49', 9.1.13.7 names no status for it - because 14.9.41.3 SR1 removes the
*> statement at COMPILE time: "The access mode of the file referenced by file-name-1 shall be either
*> sequential or dynamic."  KeyedIoBinder.BindStart screens it with COBOLNET0862.
*> BOTH keyed organizations are exercised: the screen sits before the organization split, so a
*> one-organization fixture would not show that the relative arm is covered too.
*> The reachable cells of the same table are walked by conformance:cases/9.1.13.2-Successful-completion/l1_table20_seq_relative and
*> conformance:cases/14.9.27-OPEN/l1_table20_indexed.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB325STRTRND.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXR ASSIGN TO "pb325strtrnd-x.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS RANDOM
        RECORD KEY IS IXR-KEY.
    SELECT RLR ASSIGN TO "pb325strtrnd-r.dat"
        ORGANIZATION IS RELATIVE
        ACCESS MODE IS RANDOM
        RELATIVE KEY IS RLR-KEY.
DATA DIVISION.
FILE SECTION.
FD IXR.
01 IXR-REC.
   05 IXR-KEY PIC X(4).
   05 IXR-VAL PIC X(4).
FD RLR.
01 RLR-REC PIC X(8).
WORKING-STORAGE SECTION.
01 RLR-KEY PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXR.
    START IXR KEY IS EQUAL TO IXR-KEY INVALID KEY CONTINUE END-START.
    CLOSE IXR.
    OPEN INPUT RLR.
    START RLR KEY IS EQUAL TO RLR-KEY INVALID KEY CONTINUE END-START.
    CLOSE RLR.
    STOP RUN.
