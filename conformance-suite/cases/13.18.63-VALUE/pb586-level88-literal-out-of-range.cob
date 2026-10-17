*>--- conformance-case
*> id: pb586-level88-literal-out-of-range
*> rules: [SR-13.18.63.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR2 (ALL FORMATS) - "If the category of the subject of the entry is numeric,
*> all literals in the VALUE clause shall be numeric and shall be permissible values within the range
*> indicated by the PICTURE clause or the USAGE clause." A condition-name has its conditional variable's
*> characteristics (13.18.63.4 GR19), so 12345 is outside PIC 9(2)'s range. MEASURED BEFORE: compiled clean
*> at every edition - a condition-name that can never be true - while the format-1 twin
*> 01 W2 PIC 9(2) VALUE 12345. was COBOLNET1625 (kb/Work PB586). The THROUGH-range end, the WHEN SET TO
*> FALSE literal, SR3's sign and the report-section (format 4) literal are pinned by ValueClauseScreenTests.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB586L88RNG.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9(2).
    88 C-BIG VALUE 12345.
PROCEDURE DIVISION.
MAIN.
    IF C-BIG DISPLAY "BIG" END-IF
    STOP RUN.
