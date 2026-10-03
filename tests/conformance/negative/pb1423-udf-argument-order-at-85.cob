*> reject-at: 85
*> ISO 1989:2023 8.4.3.2.4 GR2 orders the arguments of a user-defined function reference left to right, and the
*> user-defined function is a COBOL-2002 introduction (8.4.3.2 / 12.3.8): at --std 85 the reference is rejected
*> (COBOLNET0900, registry row user-function-invocation-2002) before any argument order is asked. The positive
*> half is tests/conformance/2002/udf_arg_order.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1423N85.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION PB1423NF.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-A PIC 9(4) VALUE 4.
01 WS-R PIC 9(8).
PROCEDURE DIVISION.
MAIN.
    COMPUTE WS-R = FUNCTION PB1423NF(WS-A + 0, FUNCTION PB1423NF(WS-A, 1)).
    STOP RUN.
END PROGRAM PB1423N85.
IDENTIFICATION DIVISION.
FUNCTION-ID. PB1423NF.
DATA DIVISION.
LINKAGE SECTION.
01 L-X PIC 9(4).
01 L-Y PIC 9(4).
01 L-R PIC 9(8).
PROCEDURE DIVISION USING L-X L-Y RETURNING L-R.
P.
    COMPUTE L-R = L-X + L-Y.
    GOBACK.
END FUNCTION PB1423NF.
