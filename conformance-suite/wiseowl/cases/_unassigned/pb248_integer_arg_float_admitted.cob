*>--- conformance-case
*> id: pb248_integer_arg_float_admitted
*> rules: []
*> accept: [85]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB248 - THE ADMITTED HALF of the ISO 15.3 type-6 floating-point arms, pinned so the screen cannot
*> be widened into a rejecter of legal source (the PB1 failure mode arrived at from the opposite direction).
*> 15.15.3 r1 is "Argument-1 shall be an integer", and 15.3 type 6 says what an integer ARGUMENT may be
*> (5.3.1: an intrinsic function has argument rules instead of syntax rules, so 5.5 corroborates rather than
*> governs; 5.5 2)a) points a literal operand at 8.3.3.3.2, whose closing sentence is "An integer literal is
*> a fixed-point numeric literal that contains no decimal point"). A floating-point literal is never an
*> integer literal. But type 6's OTHER
*> alternative, "an arithmetic expression that will always result in an integer value", admits any literal
*> (8.8.1.1 makes a numeric literal an arithmetic expression) whose VALUE is integral - which is why "1.0"
*> was already admitted and "1.5" was not, and the floating form must be judged the same way.
*> 8.3.3.3.3 GR5 gives the value: "the algebraic product of the value of its significand and the quantity
*> derived by raising ten to the power of the exponent".
*>
*> EXPECTED VALUES, DERIVED:
*>   6.5E1   = 6.5 x 10^1  = 65      -> integral -> admitted.
*>   6.50E1  = 6.50 x 10^1 = 65      -> integral -> admitted (trailing fraction zeros do not make it float).
*>   WS-I + (WS-F - WS-F): the floating term's NET additive coefficient is zero, so no witness valuation
*>       exists and the expression is always integral -> admitted. Value = 65 + 0 = 65.
*>   15.15.4 r1: CHAR returns "the character ... having the ordinal position specified by argument-1";
*>   15.70.4 r1: ORD returns "the ordinal position of argument-1 in the current alphanumeric program
*>   collating sequence". The round trip ORD(CHAR(n)) is therefore n, and every line prints 065.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB248FLOATADMITTED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-F USAGE COMP-2.
01 WS-I PIC 9(4) VALUE 65.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    MOVE 0.5 TO WS-F.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(6.5E1)).
    DISPLAY R.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(6.50E1)).
    DISPLAY R.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-I + (WS-F - WS-F))).
    DISPLAY R.
    STOP RUN.
