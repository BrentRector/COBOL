*>--- conformance-case
*> id: da6-group-numeric-operand
*> rules: [SR-14.9.2.3-2, SR-14.9.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.8.1.1 - "An arithmetic expression may be an identifier referencing a NUMERIC data item, a
*> numeric literal, the figurative constant ZERO ...". No group item is numeric (8.5.2.1), so G
*> is not a permissible arithmetic operand. DA6: previously ACCEPTED, decoding the digits (R=000013 for
*> a PIC X group) while a PIC 9-leaf group compiled and THREW at run time - the operand whose digits
*> were unambiguous failed and the merely-textual one succeeded. --permissive still accepts it.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDA6GROUPNUMERICO.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 A PIC X(2) VALUE "12".
01 R PIC 9(6).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = G + 1.
    STOP RUN.
