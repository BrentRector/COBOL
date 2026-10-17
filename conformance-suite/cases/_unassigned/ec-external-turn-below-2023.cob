*>--- conformance-case
*> id: ec-external-turn-below-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> VCR 15 introduction half (Annex E.3 item 9): the EC-EXTERNAL exception-condition
*> family is new in COBOL-2023 (ExceptionCatalog introducedIn 2023) — enabling it via
*> >>TURN at 2002/2014 is COBOLNET0878, never a silent no-op. (At --std 85 the >>TURN
*> directive itself is rejected first by the compiler-directive introduction gate,
*> COBOLNET0900 — a different code, so 85 is pinned by the turn-directive-2002 registry
*> row, not this fixture.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XECBELOW.
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W
           STOP RUN.
