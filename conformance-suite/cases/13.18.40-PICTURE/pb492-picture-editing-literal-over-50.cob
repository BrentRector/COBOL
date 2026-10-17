*>--- conformance-case
*> id: pb492-picture-editing-literal-over-50
*> rules: [SR-13.18.40.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.40.3 SR9, second sentence: "The total number of characters in literal-1,
*> literal-2, or literal-3 shall not exceed 50." literal-1 below is 51 characters, so the entry is
*> refused - COBOLNET1594. (A 50-character literal passes this rule and then meets the separate P14
*> multi-character render GAP, COBOLNET0899 - kb/Work PB491 - so the BOUNDARY is what this fixture
*> pins: at 51 the SR9 diagnostic is present.) SR9's FIRST sentence, the literal CLASS, is
*> conformance:negative/pb492-national-edited-literal-class (COBOLNET1955).
*> reject-at names 2023 ONLY because the PICTURE EDITING phrase is itself a COBOL-2023 introduction:
*> below 2023 the program is rejected by the phrase's own COBOLNET0900 introduction gate.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB492L50.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-1 PIC 99T99 EDITING T IS ":::::::::::::::::::::::::::::::::::::::::::::::::::".
PROCEDURE DIVISION.
MAIN.
    MOVE 1230 TO W-1
    STOP RUN.
