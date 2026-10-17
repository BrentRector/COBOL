*>--- conformance-case
*> id: pb425-boolean-zero-length-to-numeric
*> rules: [GR-14.9.25.4-3]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE COMPANION HALF OF §14.9.25.4 GR3 (kb/Work PB425): the substitution moves the VALUE, never the
*> DIAGNOSTIC. GR3 makes a boolean zero-length literal "treated as if it were the figurative constant ZERO"
*> — a GENERAL rule about what is moved — while §14.9.25.3 SR10 and Table 16 decide LEGALITY from the
*> category of the operand the programmer WROTE. Table 16's Boolean sending row gives "No" against a
*> Numeric / Numeric-edited receiving operand, so `MOVE B"" TO PIC 9(3)` is invalid at every edition that
*> has boolean literals, and COBOLNET0819 is the refusal. If the substitution were fed to the syntax
*> screens instead of applied after them, this row would read ZERO-into-numeric — a legal move — and the
*> compiler would accept source the standard forbids.
*> (Its mirror image is pinned on the positive side: tests/conformance/2002/pb425_zero_length_literal_move
*> compiles STRICT at 2002 and the same source compiles strict at 2023, where `MOVE SPACE TO PIC 9(3)` is
*> the §14.9.25.3 SR5 removal COBOLNET0902 — SR5 names only figurative constants written in the source.)
*> Below 2002 the boolean literal itself is the rejection (COBOLNET0900, boolean-data-2002), a different
*> rule, so 85 is deliberately not in the reject-at list.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB425-B-ZL-NUM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R-NUM PIC 9(3) VALUE 123.
PROCEDURE DIVISION.
MAIN.
    MOVE B"" TO R-NUM.
    DISPLAY R-NUM.
    STOP RUN.
