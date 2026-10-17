*>--- conformance-case
*> id: pb391-move-binary-long-to-alphanumeric-written
*> rules: [SR-14.9.25.3-13]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB391 (second half) - THE CONTRAST CONTROL FOR 14.9.25.3 SR8 UNDER CORRESPONDING.
*> SR8 - "If identifier-1 references a data item described with usage binary-char, binary-short, binary-long,
*> or binary-double, identifier-2 shall reference a numeric or numeric-edited item." It is one of the two
*> rules 14.9.25.3 SR10 explicitly defers to ("for all other cases not described in Syntax rules 8 and 9"),
*> so it governs BEFORE table 16 and table 16 can never answer it: to the table a BINARY-LONG item is an
*> ordinary Numeric / Integer row member, and Numeric / Integer -> Alphanumeric is "Yes".
*> 14.7.6 rule 2 sends the CORRESPONDING pairing question to "the rules for the MOVE statement", which is
*> this rule too - so the positive golden 2002/pb391_corresponding_rule2_sr8_binary pins the SILENCE for
*> exactly this operand pair: KX (USAGE BINARY-LONG) namesake KX (PIC X(5)) is not a corresponding pair and
*> the receiver keeps ZZZZZ, with no diagnostic.
*> This program writes the SAME two items as an EXPLICIT MOVE, where the rule is a "shall" and 4.2.2
*> paragraph 2 requires a compile-time mechanism: COBOLNET0819. A regression that made CORRESPONDING start
*> diagnosing, or made the written MOVE stop, breaks exactly one of the pair.
*> Before PB391's second half the two askers disagreed the OTHER way round: the written MOVE was refused
*> here while MOVE CORRESPONDING paired the same two items and overwrote the receiver with 00000.
*> Below 2002 the BINARY-LONG usage is itself introduction-gated, a different rule and a different verdict,
*> so this program is reject-at 2002 and later only.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB391MOVEBLX.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G1.
   05 KX USAGE BINARY-LONG VALUE 42.
01 G2.
   05 KX PIC X(5) VALUE "ZZZZZ".
PROCEDURE DIVISION.
MAIN.
    MOVE KX OF G1 TO KX OF G2.
    STOP RUN.
