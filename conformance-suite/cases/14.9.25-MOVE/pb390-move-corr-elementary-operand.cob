*>--- conformance-case
*> id: pb390-move-corr-elementary-operand
*> rules: [SR-14.9.25.3-12]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - ISO 14.9.25.3 SR12's FIRST half, at MOVE: "Identifier-3 and identifier-4 shall specify
*> group data items and shall not be reference-modified." ELEM is elementary. The half was already decided
*> at bind (kb/Work PB236 moved the CORRESPONDING group screen there) but only the ADD spelling had a
*> negative case - `grep -rln "MOVE CORR" tests/conformance/negative/` returned nothing - so SR12's row was
*> being asked to close on a test of 14.9.2.3 SR6. This is the MOVE case, and it sits beside
*> pb390-move-corr-reference-modified, which is the same sentence's second half.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390CORRELEM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 ELEM PIC X(3) VALUE "ABC".
01 GRP.
   05 A PIC X(3) VALUE SPACES.
PROCEDURE DIVISION.
MAIN.
    MOVE CORRESPONDING ELEM TO GRP.
    STOP RUN.
