*>--- conformance-case
*> id: pb555-false-equals-value
*> rules: [SR-13.18.63.3-27]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB555 — arm: ISO 13.18.63.3 SR27, FIRST SENTENCE (the unconditional one).
*> "The value of literal-4 shall not be equal to the value of any occurrence of literal-2."
*> That sentence carries NO class qualifier and NO THROUGH precondition, so it applies to a plain
*> singleton VALUE list like this one. literal-4 = 1 is literal-2, so `SET CN TO FALSE` would place a
*> value for which 13.18.63.4 GR20's own NOTE 3 says the condition is TRUE — the statement would be a
*> no-op that reads as a state change. COBOLNET2048.
*> Rejected at 85 as well, and the .err deliberately pins the SR27 TEXT rather than a code: at 85 the
*> phrase ALSO draws COBOLNET0900 (a COBOL-2002 introduction, row value-false-phrase-2002), so this
*> case fails the day the introduction gate starts SHORT-CIRCUITING the syntax-rule screen behind it —
*> a syntax rule applies to source as written at every edition. MEASURED at all four.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB555A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CV PIC 9 VALUE 1.
   88 CN VALUE 1 WHEN SET TO FALSE IS 1.
PROCEDURE DIVISION.
    IF CN DISPLAY "Y" ELSE DISPLAY "N" END-IF.
    STOP RUN.
