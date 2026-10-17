*>--- conformance-case
*> id: pb391-move-noninteger-to-national-written
*> rules: [SR-14.9.25.3-13]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB391 - THE CONTRAST THAT PROVES RULE 2 IS A DELEGATION AND NOT A WEAKENING.
*> ISO 14.7.6 rule 2 sends the CORRESPONDING pairing question to "the rules for the MOVE statement", and a
*> pair the MOVE rules refuse simply DOES NOT CORRESPOND - a silent non-selection, with no diagnostic. The
*> positive golden 2023/pb391_corresponding_rule2_rule4_categories pins that silence for exactly this
*> operand pair: NV (PIC 9(2)V9) namesake NV (PIC N(3)) is not a corresponding pair and the receiver keeps
*> its prior content.
*> This program writes the SAME pair as an EXPLICIT MOVE, where 14.9.25.3 SR10 governs directly and table 16
*> prints "Numeric / Noninteger -> National, National-edited = No". A syntax rule is a "shall", so 4.2.2
*> paragraph 2 requires the compile-time mechanism: COBOLNET0819.
*> The two goldens together are the whole point of PB391 - ONE table, asked by two askers, answering the
*> same cell two ways because the two framings differ (selection vs. a written statement), never because the
*> two askers carry two tables. A regression that made CORRESPONDING start diagnosing, or made the written
*> MOVE stop, breaks exactly one of the pair.
*> Below 2002 the national operand is itself introduction-gated (COBOLNET0900), which is a different rule
*> and a different verdict, so this program is reject-at 2002 and later only.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB391MOVENVN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 SRCNV PIC 9(2)V9 VALUE 3.5.
01 DSTNV PIC N(3) VALUE N"QQQ".
PROCEDURE DIVISION.
MAIN.
    MOVE SRCNV TO DSTNV.
    STOP RUN.
