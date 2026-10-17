*>--- conformance-case
*> id: pb492-move-national-edited-to-alphanumeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.25.3 SR10 and Table 16, NATIONAL-EDITED row:
*>   | National-edited | | No | No | No | Yes | No | No |
*> The ONLY "Yes" is the "National, National-edited" receiving column - an alphabetic, alphanumeric,
*> alphanumeric-edited, boolean, numeric or numeric-edited receiver is invalid. It is a SEPARATE row
*> from National (which is Yes into boolean and into the numeric column), exactly as
*> Alphanumeric-edited is a separate row from Alphanumeric: an edit mask has no de-editable value and
*> no boolean characters. The receiving COLUMN, by contrast, PAIRS the two, which is why
*> MOVE national -> national-edited is legal and pinned by
*> conformance:{2002,2014,2023}/pb492_national_edited.
*> reject-at omits 85 because the DECLARATION is already a COBOL-2002 introduction there
*> (conformance:negative/pb492-national-edited-at-85, COBOLNET0900) - a different rule and code.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB492T16.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-NE PIC NNBNN.
01 W-AN PIC X(5).
PROCEDURE DIVISION.
MAIN.
    MOVE N"ABCD" TO W-NE
    MOVE W-NE TO W-AN
    STOP RUN.
