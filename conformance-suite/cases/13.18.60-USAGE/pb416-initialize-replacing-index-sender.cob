*>--- conformance-case
*> id: pb416-initialize-replacing-index-sender
*> rules: [SR-13.18.60.3-10, SR-14.9.20.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416 — ISO 14.9.20.3 SR4 reaching 14.9.25.3 SR1, the MOVE rule about the OPERAND rather than the
*> pair: "The class of identifier-1 or identifier-2 shall not be index, message-tag, object, or pointer."
*> An index data item (8.5.2.8) may be referenced only where 13.18.60.3 SR10's closed list admits it, and a
*> MOVE sending item is not on that list — so `MOVE IX TO <an item of category NUMERIC>` is not a valid MOVE
*> statement and the REPLACING pair SR4 describes does not exist.
*> This refusal is not a pair test and short-circuits the whole REPLACING item: an index sending operand makes
*> the implicit MOVE invalid for every category the item names.
*> Measured before PB416: this compiled clean and stored 0000 — the index item's storage representation read
*> as a number, which is exactly what 8.5.2.1 Table 2's separate INDEX class exists to prevent.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NIS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       01 G.
          05 N PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING NUMERIC DATA BY IX.
           STOP RUN.
