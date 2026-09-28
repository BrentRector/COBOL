*> reject-at: 85 2002 2014 2023
*> kb/Work PB617 - a LITERAL-ONLY arithmetic expression at an integer argument position. 15.3 type 6:
*> "An arithmetic expression that will always result in an integer value or an integer data item shall be
*> specified" (cite.py: OK 15.3 6)); 15.15.3 r1: "Argument-1 shall be an integer" (cite.py: OK
*> 15.15.3 1)). 1.5 + 1 is an arithmetic expression (8.8.1.1) whose value is 2.5 for every execution, so
*> it never results in an integer. Before the fix the always-integral screen's additive walk accumulated
*> DATA ITEMS only; a literal's fraction never entered it, so this compiled clean while the bare
*> CHAR(1.5) was refused (COBOLNET1627). The admitted twin is conformance:85/pb617_integer_arg_literal_constant.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB617LITSUM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R PIC 9(3).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(1.5 + 1)).
    STOP RUN.
