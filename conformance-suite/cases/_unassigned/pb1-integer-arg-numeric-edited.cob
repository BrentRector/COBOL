*>--- conformance-case
*> id: pb1-integer-arg-numeric-edited
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> THE SIBLING OF pb1-numeric-arg-numeric-edited, ON THE 'i' (INTEGER) SCREEN RATHER THAN THE 'n' (CLASS
*> NUMERIC) ONE - and it exists because for a while the two disagreed. The 'i' arm admitted a numeric-edited
*> argument on the reasoning that 15.3's integer type 6 "admits an ARITHMETIC EXPRESSION" and a numeric-edited
*> item de-edits; the 'n' arm had ALREADY refuted that same reasoning. Both cannot be right.
*> OWNER DECISION 2026-08-02 - EXCLUDE IT EVERYWHERE. Derived, each citation --checked:
*>   . 8.8.1.1 admits "an identifier referencing a NUMERIC data item", and 8.5.2.13 says such an item "is
*>     referred to as a NUMERIC-EDITED data item" - a DISTINCT defined term. 8.5.2.1 Table 2 puts category
*>     numeric-edited in class ALPHANUMERIC (usage display) or NATIONAL, never class numeric. So it is neither
*>     the class nor the category 8.8.1.1 names, and is not an arithmetic expression.
*>   . type 6's only other alternative is "an INTEGER DATA ITEM", which it also is not.
*>   . de-editing is GRANTED by the MOVE rules (14.9.25.4 GR6d1: "de-editing establishes the operand's numeric
*>     value") and nowhere extended to arithmetic - a grant that would be unnecessary if de-editing were
*>     generally available to any numeric context.
*> Corroborated against both external oracles before landing: no NIST program depends on it, and GnuCOBOL's
*> suite exercises de-editing only under MOVE ("MOVE with de-editting to ...").
*> CHAR (15.15.3 r1) is the type-6 witness; FACTORIAL / CHAR-NATIONAL / BOOLEAN-OF-INTEGER share the arm.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1INTARGEDITED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-ED PIC Z9.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    MOVE 34 TO WS-ED.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-ED)).
    STOP RUN.
