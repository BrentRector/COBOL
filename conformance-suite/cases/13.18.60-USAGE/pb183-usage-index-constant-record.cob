*>--- conformance-case
*> id: pb183-usage-index-constant-record
*> rules: [SR-13.18.60.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR4: "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, and
*> PROGRAM-POINTER phrases shall not be specified in a data item described with the CONSTANT RECORD
*> clause, or in any item subordinate to a data item described with the CONSTANT RECORD clause."
*>
*> The subject is USAGE INDEX deliberately: SR4's list has SIX phrases where SR14's has FIVE, and
*> INDEX is the difference. `05 CFG-IX USAGE INDEX.` inside an ordinary group is LEGAL COBOL and a
*> positive golden pins that; inside a CONSTANT RECORD it is not. Two rules, two lists, two
*> predicates - this fixture is what proves the SR4 predicate is not just SR14's re-used.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183H.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CFG CONSTANT RECORD.
   05 CFG-TAG PIC X(4) VALUE "COBL".
   05 CFG-IX  USAGE INDEX.
PROCEDURE DIVISION.
MAIN.
    DISPLAY "TAG=" CFG-TAG.
    STOP RUN.
