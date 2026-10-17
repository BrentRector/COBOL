*>--- conformance-case
*> id: da7-string-into-binary
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.43.3 SR1 - every STRING identifier except the POINTER shall be usage display or
*> national. A GROUP receiver stays LEGAL (14.9.43.4 GR3a routes the transfer through the
*> alphanumeric MOVE rules, which admit a group); only an ELEMENTARY non-display receiver is
*> barred. DA7: previously a RUN-TIME throw. Edition-invariant.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGDA7STRINGINTOBIN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C PIC S9(4) COMP VALUE 0.
01 X PIC X(5) VALUE "abcde".
PROCEDURE DIVISION.
MAIN.
    STRING X DELIMITED BY SIZE INTO C.
    STOP RUN.
