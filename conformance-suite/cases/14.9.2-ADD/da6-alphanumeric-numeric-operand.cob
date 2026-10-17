*>--- conformance-case
*> id: da6-alphanumeric-numeric-operand
*> rules: [SR-14.9.2.3-2, SR-14.9.12.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.8.1.1 - an ELEMENTARY alphanumeric item is not a numeric operand either. The rule covers the
*> whole alphanumeric family, not just groups; rejecting only the group half would leave a fresh
*> inconsistency. --permissive accepts it as a digit-decoding extension.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDA6ALPHANUMERICN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X PIC X(4) VALUE "0012".
01 R PIC 9(6).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = X + 1.
    STOP RUN.
