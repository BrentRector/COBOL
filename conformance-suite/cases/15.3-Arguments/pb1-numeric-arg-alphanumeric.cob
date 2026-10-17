*>--- conformance-case
*> id: pb1-numeric-arg-alphanumeric
*> rules: [AR-15.3-10, AR-15.7.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 15.3 type 10 - "Numeric. An arithmetic expression or a numeric data item shall be specified."
*> FUNCTION ABS (15.7.3 rule 1: "Argument-1 shall be of class numeric") over an alphanumeric item was
*> accepted silently and coerced through CobolNum.FromAlphanumeric, printing garbage. The catalog row
*> declared ArgKinds "n" the whole time; nothing read it (PB1). --permissive accepts it as a coercion.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1NUMARGALNUM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(4) VALUE "ABCD".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION ABS(A).
    STOP RUN.
