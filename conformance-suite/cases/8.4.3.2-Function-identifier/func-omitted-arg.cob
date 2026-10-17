*>--- conformance-case
*> id: func-omitted-arg
*> rules: [SR-8.4.3.2.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.4.3.2 SR7 - OMITTED shall not be specified as an intrinsic-function argument (P7 Step 12:
*> the functionArgument grammar admits the 8.4.3.2.2 format's OMITTED alternative as a superset parse;
*> the binder rejects it for intrinsics with COBOLNET1544).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGFOMIT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN-PARA.
    COMPUTE R = FUNCTION MAX(OMITTED, 4).
    DISPLAY R.
    STOP RUN.
