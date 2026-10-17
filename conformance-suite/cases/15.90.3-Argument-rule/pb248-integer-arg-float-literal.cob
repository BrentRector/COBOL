*>--- conformance-case
*> id: pb248-integer-arg-float-literal
*> rules: [AR-15.90.3-1, AR-15.91.3-1, AR-15.100.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB248 - THE SECOND HALF: the LITERAL arm of the 15.3 type-6 screen was disarmed for every
*> floating-point literal. It opened `if (text.Contains('E')) return false;` with the in-code justification
*> "an E-form literal is left to the runtime", and the grammar admits one directly in a FUNCTION-argument
*> region, so FUNCTION TEST-DATE-YYYYMMDD(2.02402299E7) reached the runtime, was truncated to 20240229, and
*> answered "valid date".
*> THE RULE. 15.3 type 6 governs (5.3.1: an intrinsic function has ARGUMENT rules "instead" of syntax
*> rules, so 5.5 - scoped to "a syntax rule" - corroborates here rather than governs; it says the same
*> thing at 2)a), pointing a literal operand at 8.3.3.3.2, whose closing sentence is "An integer literal is
*> a fixed-point numeric literal that contains no decimal point", which no 8.3.3.3.3 floating-point literal
*> can be). Type 6's alternatives are "an integer data item" - which a literal is not - and "an arithmetic
*> integer value", and 8.8.1.1 makes a numeric literal an arithmetic expression, so the question the screen
*> must ask of a literal is about its VALUE, exactly as it already did for the fixed-point form ("1.0" is
*> admitted, "1.5" is not). 8.3.3.3.3 GR5 gives that value: "the algebraic product of the value of its
*> significand and the quantity derived by raising ten to the power of the exponent".
*> DERIVATION OF THIS FIXTURE'S EXPECTED VERDICT: 6.5E1 = 6.5 x 10^1 = 65.0 -> integral -> ADMITTED (it is
*> the sibling positive control, pb248-integer-arg-float-literal-integral). 6.55E1 = 65.5 -> NOT integral ->
*> REJECTED, which is what this program writes.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB248FLOATLIT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(6.55E1)).
    STOP RUN.
