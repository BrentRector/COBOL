*>--- conformance-case
*> id: stat-median-arg-class-numeric
*> rules: [AR-15.61.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §15.61.3 rule 1 (MEDIAN): "Argument-1 shall be of class numeric." It is the
*> ONLY argument rule §15.61.3 has, so any argument diagnostic this program draws
*> is that rule and no other.
*>
*> §15.61.2's general format is `FUNCTION MEDIAN ( { argument-1 } ... )`: every
*> written position IS argument-1, so the rule governs the whole variadic list.
*> This fixture puts the non-numeric operand at POSITION 2 OF 3 — the position a
*> screen that stopped at the first argument would let through. pb1-numeric-arg2-
*> alphanumeric already varies that axis for a DECLARED position (§15.77.3 r1,
*> FUNCTION REM(6 A)); what is new here is the same axis on a VARIADIC TAIL,
*> where §15.61.2's `{ argument-1 } ...` makes every written position argument-1.
*>
*> MEDIAN is not MEAN: §15.61.4 r1/r2 SELECT an argument (or average the two
*> middle ones) rather than summing, so its result is the content of a chosen
*> argument. That makes the class screen load-bearing for the RETURNED VALUE and
*> not merely for the arithmetic — a non-numeric argument admitted here can WIN
*> the selection. §8.5.2.1 Table 2 puts category alphanumeric in class
*> ALPHANUMERIC, so a PIC X(3) item holding "100" is not class numeric however
*> numeric its value looks.
*>
*> The legal complement is 2023/pb62_standard_decimal_summing_family (MEDIAN= over
*> three class-numeric arguments) and 2023/pb56_dec_carrier_intrinsics (ME=), both
*> of which must keep compiling.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1MEDI02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "100".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION MEDIAN(1, A, 9).
    STOP RUN.
