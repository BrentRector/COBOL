*>--- conformance-case
*> id: pb556-value-when-set-false-at-non-88
*> rules: [SR-13.18.63.3-33]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR33 - the FORMAT-3-ONLY phrase, which the THROUGH twins do not reach:
*> `WHEN SET TO FALSE IS literal-4` appears in format 3 and nowhere else (13.18.63.2; 13.18.63.4 GR20 is its
*> only general rule), so a level-01 entry carrying one is nonconforming source (COBOLNET2167).
*> Rejected from 2002 rather than 85 because the phrase and its SET condition-name TO FALSE statement are both
*> COBOL-2002 additions - at 85 the entry is refused earlier, by the edition band (COBOLNET0900), which is a
*> different rule and belongs to a different test.
*> MEASURED BEFORE the screen, at --std 2023 on this tree: the phrase was parsed and DROPPED in silence, the
*> program compiled clean and X held 1. (The adjudication measured the same at 2002 and 2014.)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB556WSF01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC 9 VALUE 1 WHEN SET TO FALSE IS 0.
PROCEDURE DIVISION.
MAIN.
    DISPLAY X
    STOP RUN.
