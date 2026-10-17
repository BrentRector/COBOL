*>--- conformance-case
*> id: stat-variance-arg-class-numeric
*> rules: [AR-15.98.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §15.98.3 rule 1 (VARIANCE): "Argument-1 shall be of class numeric." It is
*> the ONLY argument rule §15.98.3 has, so any argument diagnostic this program
*> draws is that rule and no other.
*>
*> §15.98.2's general format is `FUNCTION VARIANCE ( { argument-1 } ... )`: every
*> written position IS argument-1, so the rule governs the whole variadic list.
*> This fixture puts the non-numeric operand at POSITION 1, the ordinal the
*> clause names literally.
*>
*> §8.5.2.1 Table 2 puts category alphanumeric in class ALPHANUMERIC, so a
*> PIC X(3) item holding "100" is not of class numeric however numeric its value
*> looks. §15.3 argument type 10 (Numeric) admits "an arithmetic expression or a
*> numeric data item"; §8.8.1.1 admits as an arithmetic expression "an identifier
*> referencing a numeric data item", which an alphanumeric item is not. The
*> sibling axis — a NUMERIC-EDITED operand, class alphanumeric by the same Table 2
*> row — is pinned once for the family by pb1-numeric-arg-numeric-edited.
*>
*> The legal complement is 2023/pb56_dec_carrier_intrinsics (VA=, over three
*> class-numeric arguments), which must keep compiling.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1VARI01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "100".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION VARIANCE(A, 5).
    STOP RUN.
