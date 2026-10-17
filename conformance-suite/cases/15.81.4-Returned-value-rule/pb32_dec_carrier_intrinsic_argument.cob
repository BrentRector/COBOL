*>--- conformance-case
*> id: pb32_dec_carrier_intrinsic_argument
*> rules: [RV-15.81.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> AN ARITHMETIC-EXPRESSION ARGUMENT IS LEGAL IN EVERY INTRINSIC POSITION, IN EVERY ARITHMETIC MODE.
      *> 15.3 type 10 admits "An arithmetic expression or a numeric data item" and ARITHMETIC IS STANDARD-DECIMAL
      *> is source-level and legal at 2014 and 2023, so every line below is conforming COBOL.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE NONE OF IT COMPILED (fix-queue PB32/PB14).
      *> NumX carries THREE carriers - the exact scaled Int128, the CobolDec SDIDI, and binary64 - and the
      *> intrinsic renderer's arms were written for two. Under a standard arithmetic mode NumericRenderer's
      *> CombineCore and Power return Dec-carried values, so an arithmetic-expression argument arrived at
      *> MaxScaled(params Int128[]) as a raw CobolDec, which has no conversion operator. The user saw the
      *> generated C# fail on conforming source:
      *>     COMPUTE R = FUNCTION MAX(A + B, B)
      *>     error: backend compilation failed (generated C# at ...g.cs):
      *>       error CS1503: Argument 1: cannot convert from 'CobolNet.Runtime.CobolDec' to 'System.Int128'
      *> That is the PB2 shape on the Dec axis: an INTERNAL failure escaping as a backend error, phrased in C#
      *> the programmer never asked to see.
      *>
      *> ⚠ THE SPREAD IS THE POINT, AND IT IS WHY THE LANDING IS AT Arg() AND NOT IN EACH ARM. Fixing only
      *> NumericRenderer.Align recovered the variadic family that routes through it - MAX, MIN, MOD, MEDIAN -
      *> and left ABS, SIGN, INTEGER, FRACTION-PART and FACTORIAL still failing, because those arms consume
      *> Arg(...).Expr or AsInt directly. All nine are pinned here so a future arm cannot regress a subset.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB32DECARG.
       OPTIONS. ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC S9(9)V99 VALUE 3.5.
       01 B  PIC S9(9)V99 VALUE 7.25.
       01 R  PIC S9(9)V99.
       01 I  PIC S9(9).
       01 J  PIC S9(9) VALUE 3.
       PROCEDURE DIVISION.
      *> The variadic family - reached through AlignedArgs.
           COMPUTE R = FUNCTION MAX(A + B, B)
           DISPLAY "MAX=" R
           COMPUTE R = FUNCTION MIN(A + B, B)
           DISPLAY "MIN=" R
      *> ⚠ MOD's OPERANDS ARE INTEGERS, AND THIS LINE USED SCALED ONES (corrected
      *> with fix-queue PB40). 15.64.3 r1 is "Argument-1 and argument-2 shall be
      *> integers" - MOD is a 15.3 TYPE 6 position, not the type 10 this file's
      *> header cites for the rest of the family - so `FUNCTION MOD(A + B, B)`
      *> over PIC S9(9)V99 items was ILLEGAL SOURCE that this golden asserted
      *> COMPILES. It was accepted only because the argument screen tested CLASS,
      *> and a scaled numeric item is class numeric. The dec-carrier point is
      *> untouched: `J + 10` is still an arithmetic-expression argument reaching
      *> the AlignedArgs route under ARITHMETIC IS STANDARD-DECIMAL.
           COMPUTE R = FUNCTION MOD(J + 10, 7)
           DISPLAY "MOD=" R
           COMPUTE R = FUNCTION MEDIAN(A + B, B, A)
           DISPLAY "MEDIAN=" R
      *> The single-argument arms - reached through Arg().Expr, which Align never sees.
           COMPUTE R = FUNCTION ABS(A - B)
           DISPLAY "ABS=" R
           COMPUTE I = FUNCTION SIGN(A - B)
           DISPLAY "SIGN=" I
           COMPUTE I = FUNCTION INTEGER(A + B)
           DISPLAY "INTEGER=" I
           COMPUTE R = FUNCTION FRACTION-PART(A + B)
           DISPLAY "FRACTION=" R
      *> The integer-argument arm - reached through AsInt, a third route again.
           COMPUTE I = FUNCTION FACTORIAL(A - A + 3)
           DISPLAY "FACTORIAL=" I
           STOP RUN.
