*>--- conformance-case
*> id: pb560-numeric-edited-value-below-2023
*> rules: [SR-13.18.63.3-6, GR-13.18.63.4-19]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR6 - "If the item is of category numeric-edited, then, subject to Syntax rules
*> 2 and 3, literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted
*> to their numeric-edited forms according to the rules for the MOVE statement". Annex E.3.3 item 43 dates
*> that permission: "VALUE clause, numeric-edited items and numeric literals. It is now permitted to allow
*> numeric-edited data items to be assigned values specified as numeric literals" - a COBOL-2023 addition,
*> so below 2023 the version-conformance pass rejects it (COBOLNET0900).
*> This is the gating negative of tests/conformance/2023/pb560_condition_value_numeric_edited_2023, whose
*> legs all rest on that conversion; it pins the edition band the positive runs in.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB560NEVAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-A PIC ZZ9.99 VALUE 10.
PROCEDURE DIVISION.
MAIN.
    DISPLAY WS-A
    STOP RUN.
