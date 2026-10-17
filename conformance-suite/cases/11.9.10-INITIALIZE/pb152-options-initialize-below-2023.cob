*>--- conformance-case
*> id: pb152-options-initialize-below-2023
*> rules: [GR-11.9.10.4-6]
*> source-format: free
*> portable: unverified
*>---
*> The OPTIONS INITIALIZE clause is a COBOL-2023 addition (Annex E.3.3 item 33), and the introduction
*> gate that says so must keep firing below 2023 after kb/Work PB152 gave the clause its data-division
*> consumers. The fill literal here is the CONFORMING X"5A" spelling precisely so that the only thing
*> this fixture can be rejected for is the edition gate - a fixture that also violated 11.9.10.3 SR1
*> would pass for the wrong reason if the gate ever stopped firing.
*>
*> This is the edition-gate half of the PB152 landing's obligation: the new BEHAVIOUR is a >= 2023
*> branch (below 2023 the clause cannot be written, so the no-clause space/zero seed remains the
*> conformant realization of 11.9.10.4 GR6 there), and nothing about the fill work may disturb the
*> introduction gate.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB152C.
OPTIONS.
    INITIALIZE ALL TO X"5A".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(4).
PROCEDURE DIVISION.
MAIN.
    DISPLAY A.
    STOP RUN.
