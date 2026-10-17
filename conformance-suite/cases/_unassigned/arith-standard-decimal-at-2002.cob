*>--- conformance-case
*> id: arith-standard-decimal-at-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 11.9.5 / 8.8.1.5: STANDARD-DECIMAL is a COBOL-2014 keyword of the 2002 ARITHMETIC clause. With
*> the OPTIONS paragraph now parsing at 2002 (P10 Step 12), the VisitArithmeticMethod arm owns this
*> introduction edge: --std 2002 accepts the paragraph but rejects the 2014 keyword with 0900
*> (arithmetic-standard-decimal-2014); at 85 the paragraph's 0804 fires alongside it.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGSDP10AS.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W PIC 9V9(5).
PROCEDURE DIVISION.
MAIN.
    COMPUTE W = 1 / 8.
    DISPLAY W.
    STOP RUN.
