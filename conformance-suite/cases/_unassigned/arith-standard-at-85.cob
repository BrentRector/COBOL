*>--- conformance-case
*> id: arith-standard-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 11.9.5 / Annex E.2 item 21: ARITHMETIC IS STANDARD is a COBOL-2002 introduction (the 2002
*> OPTIONS paragraph's NATIVE|STANDARD clause). Below 2002 the arithmetic-standard-2002 dual-window
*> row rejects the mode with the 0900 introduction diagnostic (the paragraph's own 0804 fires too);
*> at 2023 the same row rejects with 0807 (removed - matrix-asserted). P10 Step 12.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGASP10AS.
OPTIONS.
    ARITHMETIC IS STANDARD.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9V9(5).
PROCEDURE DIVISION.
MAIN.
    COMPUTE W = 2 / 7 * 7.
    DISPLAY W.
    STOP RUN.
