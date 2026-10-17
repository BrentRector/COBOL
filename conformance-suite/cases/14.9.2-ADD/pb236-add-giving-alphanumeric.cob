*>--- conformance-case
*> id: pb236-add-giving-alphanumeric
*> rules: [SR-14.9.2.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 row SR-14.9.2.3-4 - ISO 14.9.2.3 SR4: "Identifier-3 shall reference a numeric data item or a
*> numeric-edited data item." X-ALPHA is category alphanumeric, so the GIVING resultant violates SR4.
*> The row recorded this as decided by EMIT-time code (ArithmeticEmitter.StoreArith's default guard) - the
*> program compiled clean and threw NotImplementedCobolFeatureException. The receiving-side category screen
*> that closed it landed with kb/Work PB128 (commit 6f85040d, ExpressionBinder.ScreenResultant); this fixture
*> is the WITNESS the row was missing, so the closure is pinned and not merely asserted. Its SR2 sibling
*> (an in-place TO receiver, where numeric-edited is BARRED) is pb128-add-edited-in-place - the pair is what
*> shows the editedOk axis is real and not a single blanket rule.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236ADDGIVX.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X-ALPHA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    ADD 1 2 GIVING X-ALPHA.
    STOP RUN.
