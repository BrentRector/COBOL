*>--- conformance-case
*> id: stat-midrange-arg-class-numeric
*> rules: [AR-15.62.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO §15.62.3 rule 1 (MIDRANGE): "Argument-1 shall be of class numeric." It is
*> the ONLY argument rule §15.62.3 has, so any argument diagnostic this program
*> draws is that rule and no other.
*>
*> §15.62.2's general format is `FUNCTION MIDRANGE ( { argument-1 } ... )`: every
*> written position IS argument-1, so the rule governs the whole variadic list.
*> This fixture puts the non-numeric operand at POSITION 3 OF 3 — the LAST
*> position, which is what proves the variadic tail rule and not just a declared
*> leading position is screened.
*>
*> MIDRANGE DOES NOT INHERIT MAX'S AND MIN'S LATITUDE. §15.62.4 r1 defines the
*> equivalent arithmetic expression as ((FUNCTION MAX (argument-list) + FUNCTION
*> MIN (argument-list)) / 2), and MAX/MIN admit alphanumeric arguments under
*> their own §15.59.3 r1 / §15.63.3 r1 negative class lists. MIDRANGE's own r1
*> does not: it demands class NUMERIC outright, so an alphanumeric argument that
*> would be legal inside a bare FUNCTION MAX is illegal here. The equivalent
*> expression states the VALUE, never the admissible arguments.
*>
*> §8.5.2.1 Table 2 puts category alphanumeric in class ALPHANUMERIC, so the
*> PIC X(3) item below is not class numeric however numeric its value looks. The
*> legal complement is 2023/pb62_standard_decimal_summing_family (MIDR=), which
*> must keep compiling.
IDENTIFICATION DIVISION.
PROGRAM-ID. L1MIDR03.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(3) VALUE "100".
01 R PIC S9(6)V99.
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FUNCTION MIDRANGE(1, 2, A).
    STOP RUN.
