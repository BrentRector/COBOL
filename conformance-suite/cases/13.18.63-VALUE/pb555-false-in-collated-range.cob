*>--- conformance-case
*> id: pb555-false-in-collated-range
*> rules: [SR-13.18.63.3-27, SR-13.18.63.3-32]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB555 — arm: ISO 13.18.63.3 SR27 b), the COLLATED range. "when literal-2 is of class
*> alphanumeric or national, and the runtime collating sequence is known, the value of literal-4 shall
*> not be equal to the value of any literal-2 or any value in the range of any occurrence of literal-2
*> through literal-3, inclusive."
*> ⛔ THIS CASE IS UNREACHABLE UNDER THE NATIVE SEQUENCE, WHICH IS THE POINT. Under MYALPH the ordinals
*> are Z=1, Y=2, X=3, A=4, so "Z" THRU "X" is an ASCENDING range that CONTAINS "Y" — and literal-4 =
*> "Y" is therefore a value the condition-name is TRUE for. Under the native sequence the same written
*> range is INVERTED ('Z'=0x5A collates after 'X'=0x58), 14.7.8 rule 2 makes it EMPTY, and no literal-4
*> could ever be inside it: a compiler that ignored the IN alphabet-name-1 phrase, or that weighed the
*> range natively, would accept this program. It pins that the SR27 screen consults the sequence
*> 14.7.8 rule 2 gives the range, through the SAME CobolCollation.Compare the generated program uses.
*> COBOLNET2048. Its conforming twin (literal-4 = "A", outside the range) is exercised by
*> tests/conformance/2002/pb555_value_false_phrase's native-ordered leg.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB555B.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET MYALPH IS "Z" "Y" "X" "A".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CV PIC X VALUE "Y".
   88 CN VALUE "Z" THRU "X" IN MYALPH WHEN SET TO FALSE IS "Y".
PROCEDURE DIVISION.
    IF CN DISPLAY "Y" ELSE DISPLAY "N" END-IF.
    STOP RUN.
