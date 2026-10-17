*>--- conformance-case
*> id: pb492-national-edited-value-literal-class
*> rules: [SR-13.18.63.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR5: "If the item is of category national or national-edited, literals in
*> the VALUE clause shall be national literals." The subject here is category national-edited
*> (13.18.40.4 GR10), so the alphanumeric "AB CD" is refused - COBOLNET0898. The legal spelling,
*> VALUE N"AB CD", is pinned by conformance:{2002,2014,2023}/pb492_national_edited.
*> reject-at omits 85 because the DECLARATION is already a COBOL-2002 introduction there
*> (conformance:cases/13.18.40-PICTURE/pb492-national-edited-at-85, COBOLNET0900) - a different rule and code.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB492VAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-NE PIC NNBNN VALUE "AB CD".
PROCEDURE DIVISION.
MAIN.
    DISPLAY W-NE
    STOP RUN.
