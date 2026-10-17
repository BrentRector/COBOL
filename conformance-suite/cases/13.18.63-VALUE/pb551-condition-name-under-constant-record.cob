*>--- conformance-case
*> id: pb551-condition-name-under-constant-record
*> rules: [SR-13.18.63.3-25]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR25 - "A format 3, 4, or 5 VALUE clause shall not be specified in any data
*> description entry that contains the CONSTANT RECORD clause, or in any data description entry subordinate
*> to a data description entry that contains the CONSTANT RECORD clause." CONSTANT RECORD is a COBOL-2002
*> introduction (below it the clause itself draws COBOLNET0900). MEASURED BEFORE: compiled clean and F was
*> live - the program displayed YES (kb/Work PB551). The record's FORMAT-1 VALUE on X stays legal; only the
*> level-88 is refused. (COBOLNET2406)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB551CR88.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CR CONSTANT RECORD.
    05 X PIC 9 VALUE 1.
    88 F VALUE 1.
PROCEDURE DIVISION.
MAIN.
    IF F DISPLAY "YES" ELSE DISPLAY "NO" END-IF
    STOP RUN.
