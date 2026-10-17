*>--- conformance-case
*> id: stat-standard-deviation-arg-class-numeric
*> rules: [AR-15.86.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §15.86.3 rule 1 (STANDARD-DEVIATION): "Argument-1 shall be of class
*> numeric." It is the ONLY argument rule §15.86.3 has, so any argument
*> diagnostic this program draws is that rule and no other.
*>
*> §15.86.2's general format is `FUNCTION STANDARD-DEVIATION ( { argument-1 }
*> ... )`: every written position IS argument-1, so the rule governs the whole
*> variadic list. This fixture puts the non-numeric operand at POSITION 4 OF 4 —
*> the deepest position any §15.3 class fixture exercises, which is what shows
*> the rule riding a variadic TAIL rather than a fixed list of ordinals.
*>
*> THE RULE IS THE FUNCTION'S OWN, NOT VARIANCE'S. §15.86.4 r1 defines the value
*> as (FUNCTION SQRT (FUNCTION VARIANCE (argument-list))), so it is tempting to
*> read the argument admissibility off VARIANCE's §15.98.3 r1 as well; the
*> equivalent arithmetic expression states the VALUE and never the admissible
*> arguments, and §15.86.3 r1 is a rule in its own right that must be cited and
*> enforced on its own clause. A diagnostic citing §15.98.3 here would be the
*> inherited-citation failure.
*>
*> §8.5.2.1 Table 2 puts category alphanumeric in class ALPHANUMERIC, so a
*> PIC X(3) item holding "100" is not class numeric however numeric its value
*> looks. The legal complement is 2023/pb56_dec_carrier_intrinsics (SD=, over
*> three class-numeric arguments), which must keep compiling.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1SDEV04.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "100".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION STANDARD-DEVIATION(1, 2, 3, A).
    STOP RUN.
