*>--- conformance-case
*> id: pb38_standard_mode_beats_float_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> UNDER A STANDARD ARITHMETIC MODE THE MODE DECIDES THE CARRIER, NOT THE ARGUMENT'S USAGE.
      *> 15.4.1 rule 1 is unconditional under the standard modes - a function WITH an equivalent arithmetic
      *> expression shall RETURN THE VALUE of that expression - and 8.8.1.5.2 rule 1 converts every fixed-point
      *> operand into a standard-decimal intermediate EXACTLY. A COMP-2 argument converts IN per 8.8.1.5.1; it
      *> does not drag the other operands out into binary64.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE ONE FLOAT ARGUMENT DEMOTED THE WHOLE LIST (fix-queue PB38).
      *> IntrinsicRenderer.RenderNum routed on AnyRealArgument with NO arithmetic-mode guard, while
      *> NumericRenderer.CombineCore, NumericRenderer.Power and ConditionRenderer all test the mode FIRST - the
      *> ordering COBOLNET_NUMERIC_DESIGN.md D3 states in words, "the mode branch runs BEFORE the D16 float
      *> branch". RenderNum was the one renderer that did not, so:
      *>     MEDIAN(H1 H2 H3 F1 F1) -> 100000000000000004.76   where the SDIDI-exact answer is 100000000000000001
      *>     MAX(H1 H2 H3 F1)       -> 100000000000000004.76   where the SDIDI-exact answer is 100000000000000003
      *> The three 18-digit operands all collapse to ONE binary64 - the ulp at 1e17 is 16 - so they compare EQUAL
      *> and the 8.8.4.2.4 comparison the clause mandates never happens. The failure mode is COLLAPSE, not
      *> inversion, which is why the sorted position still tracked while the selected value did not.
      *>
      *> ⚠ THE ALL-FIXED CONTROL IS HALF THE EVIDENCE. The same three operands without the COMP-2 pair were
      *> always exact, in the same program and the same mode - so the error was injected by the ROUTE, not by the
      *> operands, and a golden that omitted the control could not show that.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB38SDMODE.
       OPTIONS. ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H1 PIC S9(18) VALUE 100000000000000001.
       01 H2 PIC S9(18) VALUE 100000000000000003.
       01 H3 PIC S9(18) VALUE 100000000000000002.
       01 F1 USAGE COMP-2 VALUE 1.
       01 R  PIC S9(18)V99.
       PROCEDURE DIVISION.
      *> The control: no float in the list. This was always correct.
           COMPUTE R = FUNCTION MEDIAN(H1 H2 H3)
           DISPLAY "CONTROL-MEDIAN=" R
      *> Five occurrences sorted are 1, 1, H1, H3, H2 - argument-a is H1, a fixed-point operand 8.8.1.5.2 r1
      *> converts into an SDIDI exactly, and 15.4.1 r1 requires that value.
           COMPUTE R = FUNCTION MEDIAN(H1 H2 H3 F1 F1)
           DISPLAY "MEDIAN-FLOAT=" R
      *> MAX must select H2 = 100000000000000003, not a collapsed binary64 neighbourhood.
           COMPUTE R = FUNCTION MAX(H1 H2 H3 F1)
           DISPLAY "MAX-FLOAT=" R
      *> MIN over the same list selects the float itself, which is exactly representable.
           COMPUTE R = FUNCTION MIN(H1 H2 H3 F1)
           DISPLAY "MIN-FLOAT=" R
      *> The three distinct operands must still COMPARE distinct with a float present (8.8.4.2.4).
           IF FUNCTION MAX(H1 H2 H3 F1) = FUNCTION MIN(H1 H2 H3)
               DISPLAY "COLLAPSED=YES"
           ELSE
               DISPLAY "COLLAPSED=NO"
           END-IF
      *> ⚠ THE WHOLE AlignedArgs FAMILY SHARES THE ROUTE, SO THE WHOLE FAMILY IS PINNED (rule 4 - every bug is a
      *> pattern). A golden covering three of eight functions would leave five arms free to regress on the exact
      *> line this fix changed.
           COMPUTE R = FUNCTION RANGE(H1 H2 H3 F1)
           DISPLAY "RANGE-FLOAT=" R
           COMPUTE R = FUNCTION MIDRANGE(H1 H2 F1)
           DISPLAY "MIDRANGE-FLOAT=" R
           COMPUTE R = FUNCTION ORD-MAX(H1 H2 H3 F1)
           DISPLAY "ORDMAX-FLOAT=" R
           COMPUTE R = FUNCTION ORD-MIN(H1 H2 H3 F1)
           DISPLAY "ORDMIN-FLOAT=" R
           STOP RUN.
