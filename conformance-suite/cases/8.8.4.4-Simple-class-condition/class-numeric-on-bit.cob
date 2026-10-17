*>--- conformance-case
*> id: class-numeric-on-bit
*> rules: [SR-8.8.4.4.3-8]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.8.4.4.3 SR8: NUMERIC requires an operand whose usage is display or national or whose
*> category is numeric - a USAGE BIT boolean is none of these.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB10.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-BIT PIC 1(4) USAGE BIT VALUE B"0101".
PROCEDURE DIVISION.
MAIN.
    IF W-BIT IS NUMERIC DISPLAY "N".
    STOP RUN.
