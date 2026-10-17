*>--- conformance-case
*> id: pb495-inherited-usage-forbids-picture
*> rules: [GR-13.18.60.4-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB495 — ISO 13.16.3 SR8: "The PICTURE clause shall not be specified for the subject of a RENAMES
*> clause or for an item whose usage is binary-char, binary-short, binary-long, binary-double, float-short,
*> float-long, float-extended, index, message-tag, object reference, pointer, function-pointer, or
*> program-pointer." A's usage IS binary-long: 13.18.60.4 GR1 says a group-level USAGE clause "applies only
*> to each elementary item in the group", so the clause written on G is A's clause, and SR8 forbids A's
*> PICTURE exactly as it forbids `05 A PIC 9(2) USAGE BINARY-LONG.` -- the same verdict, the same
*> COBOLNET0870, for the same item written two ways. Before PB495 the inherited spelling was accepted in
*> SILENCE and A bound as a 2-byte USAGE DISPLAY item, where BINARY-LONG is 4 bytes.
*> BINARY-LONG is a COBOL-2002 addition, so 85 rejects it through the introduction gate (COBOLNET0900)
*> rather than through this rule and is not claimed here.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB495B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G USAGE BINARY-LONG.
   05 A PIC 9(2).
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
