*>--- conformance-case
*> id: pb495-sr2-contradicting-group-usage
*> rules: [SR-13.18.60.3-2, SR-13.18.60.3-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB495 — ISO 13.18.60.3 SR2: "If the USAGE clause is written in the data description entry for a
*> group item, it may also be written in the data description entry for any subordinate elementary item or
*> group item, but the same usage shall be specified in both entries." B writes USAGE DISPLAY inside a group
*> that wrote USAGE COMPUTATIONAL, so the two entries do not specify the same usage and the source is
*> nonconforming at every edition (COMPUTATIONAL, DISPLAY and the group-level USAGE clause are all COBOL-85).
*> The rule had NO site in the compiler before PB495: the nearer clause simply won and the outer one was
*> discarded in silence, so B bound as a 4-byte DISPLAY item and the group measured 6 bytes where the
*> conforming spelling gives 4 -- a width no written clause asked for, which a REDEFINES window, a record
*> image or a CALL boundary would then key on.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB495A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G USAGE COMPUTATIONAL.
   05 A PIC 9(4).
   05 B PIC 9(4) USAGE DISPLAY.
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
