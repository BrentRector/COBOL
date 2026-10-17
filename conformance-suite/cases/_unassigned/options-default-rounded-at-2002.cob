*>--- conformance-case
*> id: options-default-rounded-at-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 11.9.6: DEFAULT ROUNDED is a COBOL-2014 clause of the 2002 OPTIONS paragraph. The paragraph
*> gate moving to 2002 (P10 Step 12) must NOT silently admit the 2014-only clauses at 2002 - each
*> carries its own introduction arm (options-default-rounded-2014 here; likewise INTERMEDIATE
*> ROUNDING / ENTRY-CONVENTION / FLOAT-BINARY / FLOAT-DECIMAL / INITIALIZE).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDRP10AS.
OPTIONS.
    DEFAULT ROUNDED MODE IS NEAREST-EVEN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    COMPUTE W ROUNDED = 5 / 2.
    DISPLAY W.
    STOP RUN.
