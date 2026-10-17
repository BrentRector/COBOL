*>--- conformance-case
*> id: pb236-add-giving-reference-modified
*> rules: [SR-14.9.2.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 row SR-14.9.2.3-4, the arm that escaped even the run-time guard. ISO 8.4.3.3.4 GR6c makes a
*> reference-modified result of a NUMERIC item class and category ALPHANUMERIC, so `N(1:2)` violates
*> 14.9.2.3 SR4 - but the old guard read `target.Item.Pic` and PlaceDecorator.Item returns the INNER item, so
*> the slice's category read back as Numeric and the guard PASSED. The store then handed a numeric value to
*> PlaceRenderer's RefModPlace arm, which SPLICES A STRING: the failure was a raw Roslyn type error on
*> generated user source, not even the named loud the record relied on.
*> ScreenResultant (kb/Work PB128) tests the PLACE, not the decorated item, which is why the arm is now
*> caught; this fixture pins that it stays caught, and the diagnostic quotes GR6c so the reason is the rule.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236ADDGIVR.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 N PIC 9(5).
PROCEDURE DIVISION.
MAIN.
    ADD 1 2 GIVING N(1:2).
    STOP RUN.
