*>--- conformance-case
*> id: pb646-national-form-numeric-at-85
*> rules: [GR-13.18.40.4-8, SR-13.18.60.3-12]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60.3 SR12 - the NATIONAL-FORM shapes (a numeric, numeric-edited or boolean
*> PICTURE under USAGE NATIONAL) are national data, and national data is a COBOL-2002 introduction
*> (construct registry row national-data-2002), so below 2002 the version-conformance pass rejects
*> each declaration with COBOLNET0900 naming COBOL-2002. The companion POSITIVE goldens are
*> conformance:{2002,2014,2023}/pb646_national_form_numeric.
*> The gate keys on the RESOLVED usage, not only on the written keyword, so the GROUP-INHERITED
*> spelling below (13.18.60.4 GR1) is rejected exactly as the written one is - kb/Work PB646.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB646G85.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N9 PIC 9(3) USAGE NATIONAL.
01 W-NE PIC ZZ9 USAGE NATIONAL.
01 W-NB PIC 1(4) USAGE NATIONAL.
01 W-GG USAGE NATIONAL.
   05 W-GL PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
