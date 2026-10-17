*>--- conformance-case
*> id: pb646-numeric-edited-value-national-class
*> rules: [SR-13.18.63.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR7, the OTHER direction - 8.5.2.1 Table 2 puts "Numeric-edited (if usage
*> is display)" in class ALPHANUMERIC, so a usage-DISPLAY numeric-edited item takes an ALPHANUMERIC
*> literal and this NATIONAL one does not conform: COBOLNET0898. SR7 bites both ways, and neither way
*> was observable while 13.18.60.3 SR12's national-form numeric-edited item was staged loud at
*> COBOLNET0899 - the class of a numeric-edited item could only ever be alphanumeric (kb/Work PB646).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB646VC2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-DE PIC ZZ9 VALUE N"  1".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
