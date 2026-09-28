*> kb/Work PB617 - THE ADMITTED HALF of the literal-constant witness: an integer argument written as an
*> arithmetic expression whose literal terms carry fractions is judged on the VALUE of the expression,
*> never on the presence of a fraction in some literal.
*> 15.3 type 6: "An arithmetic expression that will always result in an integer value or an integer data
*> item shall be specified" (cite.py: OK 15.3 6)); 15.15.3 r1: "Argument-1 shall be an integer"
*> (cite.py: OK 15.15.3 1)). 8.8.1.1 makes each numeric literal an arithmetic expression, and the value of
*> a sum of literals is the algebraic sum of their values.
*>
*> EXPECTED VALUES, DERIVED (15.15.4 r1 CHAR returns the character at ordinal position argument-1 and
*> 15.70.4 r1 ORD returns the ordinal position of its argument, so ORD(CHAR(n)) = n):
*>   64.5 + 0.5          = 65                      -> integral -> admitted, prints 065
*>   67 - 1              = 66                      -> admitted, prints 066
*>   WS-I + 1.5 + 1.5    = 64 + 3 = 67             -> the literal terms sum to the integer 3 and WS-I is an
*>                                                    integer item -> always integral -> prints 067
*>   2 ** 6 + 4          = 68                      -> admitted, prints 068
*>   0.5 * 138           = 69                      -> admitted, prints 069
IDENTIFICATION DIVISION.
PROGRAM-ID. PB617ADMITTED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-I PIC 9(4) VALUE 64.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(64.5 + 0.5)).
    DISPLAY R.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(67 - 1)).
    DISPLAY R.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-I + 1.5 + 1.5)).
    DISPLAY R.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(2 ** 6 + 4)).
    DISPLAY R.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(0.5 * 138)).
    DISPLAY R.
    STOP RUN.
