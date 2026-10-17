*>--- conformance-case
*> id: pb492-national-edited-literal-class
*> rules: [SR-13.18.40.3-9]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.40.3 SR9, first sentence: "If USAGE IS NATIONAL is specified for the subject of
*> the entry or if character-string-1 contains the symbol 'N', literal-1, literal-2, and literal-3
*> shall be national literals. Otherwise, literal1, literal-2, and literal-3 shall be alphanumeric
*> literals." Character-string-1 here contains 'N', so literal-1 shall be a national literal and the
*> alphanumeric ":" is refused - COBOLNET1955. The legal spelling, N":", is
*> conformance:2023/pb492_national_edited_editing.
*> reject-at names 2023 ONLY because the PICTURE EDITING phrase is itself a COBOL-2023 introduction:
*> below 2023 this program is rejected by the phrase's own COBOLNET0900 introduction gate
*> (conformance:negative/pb490-picture-editing-at-85), which is a different rule and a different code.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB492SR9.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-NE PIC NNTNN EDITING T IS ":".
PROCEDURE DIVISION.
MAIN.
    MOVE N"ABCD" TO W-NE
    STOP RUN.
