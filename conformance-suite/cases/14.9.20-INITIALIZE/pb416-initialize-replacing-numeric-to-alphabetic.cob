*>--- conformance-case
*> id: pb416-initialize-replacing-numeric-to-alphabetic
*> rules: [SR-14.9.20.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416 — ISO 14.9.20.3 SR4, second paragraph: "For each of the other categories specified in the
*> REPLACING phrase, a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the
*> specified category as the receiving operand shall be valid." 14.9.20.4 GR4 is what makes it a rule about
*> INITIALIZE — the implicit statements ARE MOVEs.
*> 14.9.25.3 Table 16 marks the Numeric/Integer row against the Alphabetic column "No", so
*> `MOVE 5 TO <an item of category ALPHABETIC>` is not a valid MOVE and the REPLACING pair is refused. The
*> receiving operand of that hypothetical MOVE is the CATEGORY the phrase names, not any item G happens to
*> contain, so the rule is decided over the phrase alone.
*> Measured before PB416: this compiled clean and STORED, leaving a PIC A(4) item holding "5   " — a value no
*> conforming program can produce, and one a later class-ALPHABETIC test on the item reads as false. The
*> explicit-MOVE form of the same cell was already COBOLNET0819; the two now ask ONE screen (MoveTable16).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NNA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 AB PIC A(4) VALUE "wxyz".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING ALPHABETIC DATA BY 5.
           STOP RUN.
