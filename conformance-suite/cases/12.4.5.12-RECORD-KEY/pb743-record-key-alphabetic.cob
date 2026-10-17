*>--- conformance-case
*> id: pb743-record-key-alphabetic
*> rules: [SR-12.4.5.12.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.12.3 SR2's category arm over the OTHER excluded neighbour of category
*> alphanumeric. PIC A is category ALPHABETIC (8.5.2.3), a category of its own; 8.8.4.2.4's "A class
*> alphabetic operand shall be treated as though it were an operand of class alphanumeric" is a
*> COMPARISON rule and not a category identity, so an alphabetic key is not "of category
*> alphanumeric" and SR2 refuses it. This case exists because the storage model folds PIC A into
*> PicCategory.Alphanumeric: without it, a screen written as a bare category comparison would pass
*> and nothing would say so (kb/Work PB743; the model carries PicInfo.IsAlphabetic for exactly this).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB743RKALPHA.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb743rkalpha.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS RANDOM
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC A(5).
   05 IX-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
