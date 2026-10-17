*>--- conformance-case
*> id: pb248-integer-expr-float-term
*> rules: [AR-15.90.3-1, AR-15.91.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB248 - THE THIRD SITE, and the one the note did not name: the same missing "fixed-point"
*> conjunct disarmed the EXPRESSION half of 15.3 type 6 as well as its bare-operand half. The
*> always-integral screen walks the additive spine and accumulates each non-integral term's signed leaf
*> count; it recognized a scale>0 PICTURE item and nothing else, so a floating-point term slid through
*> `FUNCTION CHAR(WS-F + 1)` exactly as the bare item slid through `FUNCTION CHAR(WS-F)`.
*> THE WITNESS. 15.3 type 6 admits an arithmetic expression "only when it ALWAYS results in an integer
*> value". For a net additive coefficient c /= 0 over a floating-point leaf, hold every other leaf at zero
*> and value the leaf at 10^-k with 10^k > |c|: c x 10^-k is not an integer. No granularity test applies -
*> a floating-point item's value set is finer than any decimal granularity (14.6.8.3) - and applying the
*> fixed-point one would DISARM the arm, because a floating-point profile carries Scale 0 and |c| mod 10^0
*> is zero for every c.
*> FAIL-OPEN IS PRESERVED: `WS-F - WS-F` and `WS-F - (WS-F * 1)` net to zero and stay legal; the sibling
*> positive control pb248-integer-arg-float-literal-integral pins that half.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB248FLOATTERM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-F USAGE COMP-1.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    MOVE 64.5 TO WS-F.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-F + 1)).
    STOP RUN.
