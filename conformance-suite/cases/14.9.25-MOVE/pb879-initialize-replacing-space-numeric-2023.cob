*>--- conformance-case
*> id: pb879-initialize-replacing-space-numeric-2023
*> rules: [SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.20.3 SR4: "For each of the other categories specified in the REPLACING phrase, a MOVE
*> statement with identifier-2 or literal-1 as the sending item and an item of the specified category as the
*> receiving operand shall be valid." At COBOL-2023 MOVE SPACE TO <a numeric item> is not: 14.9.25.3 SR5
*> prohibits "the move of an alphanumeric figurative constant (SPACE, ...) to either a numeric item or a
*> numeric-edited item" (a removal - Annex E.2 item 1 - so it was permitted through 2014; the positive twin
*> is tests/conformance/85/pb880_initialize_replacing_figurative_numeric).
*> kb/Work PB879: SR5's edition rows lived in VersionConformancePass.GateMove, reachable only from a bound
*> MOVE node INITIALIZE never builds, so this compiled clean at 2023 while the explicit MOVE was COBOLNET0902.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB879N23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9(3).
       PROCEDURE DIVISION.
           INITIALIZE G REPLACING NUMERIC DATA BY SPACE
           DISPLAY "N=[" N "]"
           STOP RUN.
