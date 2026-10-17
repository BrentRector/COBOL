*>--- conformance-case
*> id: da7-inspect-binary-operand
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.22.3 SR1 - INSPECT identifier-1 shall be an alphanumeric/national GROUP item, or an
*> ELEMENTARY item of usage display or national. An elementary COMP item has no character image.
*> DA7: this was previously a RUN-TIME throw, so the illegal program compiled clean and crashed
*> only when control reached the statement. Edition-invariant - SR1 is unchanged 85..2023.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDA7INSPECTBINARY.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C PIC S9(4) COMP VALUE 0.
01 X PIC X(5) VALUE "abcde".
PROCEDURE DIVISION.
MAIN.
    INSPECT C REPLACING ALL "1" BY "2".
    STOP RUN.
