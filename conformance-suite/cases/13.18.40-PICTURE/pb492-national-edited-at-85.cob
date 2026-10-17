*>--- conformance-case
*> id: pb492-national-edited-at-85
*> rules: [GR-13.18.40.4-10]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.5.2.11 / 13.18.40.4 GR10 - category NATIONAL-EDITED is a COBOL-2002 introduction
*> (construct registry row national-edited-2002), so below 2002 the version-conformance pass rejects
*> the declaration with COBOLNET0900 naming COBOL-2002. The companion POSITIVE goldens are
*> conformance:{2002,2014,2023}/pb492_national_edited, which pin the category's rendering.
*> EXACTLY ONE 0900 is expected on the entry even though the item is ALSO class and category national
*> (8.5.2.1 Table 2): the picture-shape gate is the finer identity and the usage gate does not fire a
*> second one - RepositoryPrototypeEditionTests.NationalEditedPicture_At85_ExactlyOne0900.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB492G85.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-NE PIC NNBNN.
PROCEDURE DIVISION.
MAIN.
    MOVE N"ABCD" TO W-NE
    STOP RUN.
