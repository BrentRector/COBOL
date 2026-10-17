*>--- conformance-case
*> id: program-pointer-with-picture
*> rules: [SR-13.16.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 SR8 - the PICTURE clause shall not be specified for an item whose usage is
*> program-pointer (COBOLNET0881).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPP03.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PP PIC X(8) USAGE PROGRAM-POINTER.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
