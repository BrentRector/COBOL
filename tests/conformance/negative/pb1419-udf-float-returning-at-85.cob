*> reject-at: 85
*> ISO 1989:2023 8.4.3.2.4 GR1 gives a user-defined function's temporary the description, class and category of its
*> RETURNING item with no category excluded (so a FLOAT-LONG result is a legal function-identifier, positive half
*> tests/conformance/2002/udf_returning_every_category.cob), but the user-defined function itself is a COBOL-2002
*> introduction: at --std 85 the reference is rejected (COBOLNET0900, registry row user-function-invocation-2002).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1419N85.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB1419NF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-FIX PIC 9(4)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE WS-FIX = FUNCTION PB1419NF(3).
    STOP RUN.
END PROGRAM PB1419N85.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB1419NF.
DATA DIVISION.
LINKAGE SECTION.
01 L-X PIC 9(4).
01 L-R USAGE FLOAT-LONG.
PROCEDURE DIVISION USING L-X RETURNING L-R.
P.
    COMPUTE L-R = L-X / 2.
    GOBACK.
END FUNCTION PB1419NF.
