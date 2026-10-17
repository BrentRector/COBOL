*>--- conformance-case
*> id: l1_smallest_algebraic_float_increment
*> rules: [RV-15.83.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.83.4 r2 on a FLOATING-POINT argument-1 — the returned-value rule this row is about.
      *> "The value returned is equal to the positive algebraic value of smallest finite magnitude that may
      *> be used to increment argument-1."
      *> (cite.py --check 15.83.4 "The value returned is equal to the positive algebraic value of smallest
      *> finite magnitude that may be used to increment argument-1" -> OK, §15.83.4 rule 2.)
      *>
      *> WHAT r2 NAMES FOR A FLOAT CARRIER, DERIVED BEFORE THE RUN. "May be used to increment argument-1"
      *> is a property of the item's REPRESENTATION, not of any value it happens to hold: the smallest
      *> finite positive magnitude the format can hold at all, which for an IEEE 754 binary format is its
      *> smallest positive SUBNORMAL.
      *> ⚠ THE STANDARD DOES NOT PIN THE REPRESENTATION OF THESE TWO USAGES, AND THAT IS PART OF THE
      *> DERIVATION RATHER THAN A HOLE IN IT. §13.18.60.4 GR13 says the usages float-short, float-long and
      *> float-extended "define signed numeric data items that are held in a floating-point format suitable
      *> for the machine on which the runtime module is to run. The size and permitted range of values for
      *> these fields is defined by the implementor", and GR21 repeats it for representation and length. So
      *> r2's answer for a COMP-1 / COMP-2 argument is fixed by THIS implementation's determination, which
      *> docs/CONFORMANCE.md item DOC-A.1-207 states: FLOAT-SHORT is IEEE 754 binary32, FLOAT-LONG and
      *> FLOAT-EXTENDED are IEEE 754 binary64. (The usages the STANDARD pins itself are FLOAT-BINARY-32/-64,
      *> §13.18.60.4 GR14/GR15 — and §15.83.3 r2 bars exactly those under the arithmetic mode this program
      *> runs in, which is why the probes are written on COMP-1/COMP-2.) Hence
      *>   binary32  2^-149  = 1.40129846432481707092372958328991613128...E-45
      *>   binary64  2^-1074 = 4.94065645841246544176568792868221372365...E-324
      *> and the §15.83.4 NOTE's own fixed-point table (S999 -> +1, 99V9(3) -> +.001) is the same rule read
      *> on the other kind of carrier: the format minimum, never the ULP of a particular value.
      *>
      *> ⛔ THE BOUNDS ARE TWENTY SIGNIFICANT DIGITS WIDE, AND THAT IS THE POINT. The pre-existing witness
      *> conformance:cases/15.43.3-Argument-rules/pb122_smallest_algebraic_float brackets the binary64 answer as "> 0 and
      *> < 5.0E-324", which any positive number below the subnormal satisfies, and the binary32 answer to
      *> three digits. Those bounds prove the ADMISSION of a float argument-1 (§15.83.3 r2's arm) and are
      *> the right test for it; they do not pin the VALUE r2 names, which is this row's rule. A window one
      *> unit wide in the twentieth digit is met by exactly one number, and no other constant in the fold —
      *> the binary64 minimum NORMAL 2.2250738585072014E-308, the decimal carrier's 1E-28, or a rounded
      *> 1.4E-45 — lies inside either window.
      *>
      *> STANDARD-DECIMAL arithmetic is in effect (OPTIONS, §11.9.5), for two reasons that both matter.
      *> (1) The comparisons then run on the 34-digit decimal128 intermediate (§8.8.1.5.2), so a twenty-digit
      *> bound is actually decidable; under native they would be rounded to binary64, where the binary64
      *> window collapses onto a single subnormal and stops discriminating. (2) §15.83.3 r2 bars only the
      *> §3.166 STANDARD BINARY usages under this mode, and COMP-1 / COMP-2 are not among them, so these two
      *> arguments are legal source — the arm kb/Work PB122 restored after a blanket refusal that leaned on
      *> §15.83.3 r4's NATIVE-only implementor latitude to reject under a standard mode.
      *> §15.83.4 r1's IN-ARITHMETIC-RANGE screen passes for both: binary32's extremes are 1E+38 / 1E-45 and
      *> binary64's 1E+308 / 1E-324, all inside the decimal128 intermediate's +6145 / -6176 range.
      *>
      *> Hand-derived expectations:
      *>   S32  1.4012984643248170709E-45  < FUNCTION SMALLEST-ALGEBRAIC(S) < 1.4012984643248170710E-45
      *>   D64  4.9406564584124654417E-324 < FUNCTION SMALLEST-ALGEBRAIC(D) < 4.9406564584124654418E-324
      *>   FIX  the NOTE's own second row on the same run: a 99V9(3) item increments by .001 exactly, so the
      *>        fixed-point arm and the float arm are measured by one program and neither can be read as the
      *>        other's coverage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SMALGF.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE COMP-1.
       01 D USAGE COMP-2.
       01 F PIC 99V9(3).
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION SMALLEST-ALGEBRAIC(S) > 1.4012984643248170709E-45
              AND FUNCTION SMALLEST-ALGEBRAIC(S) < 1.4012984643248170710E-45
               DISPLAY "S32 OK" ELSE DISPLAY "S32 BAD" END-IF
           IF FUNCTION SMALLEST-ALGEBRAIC(D) > 4.9406564584124654417E-324
              AND FUNCTION SMALLEST-ALGEBRAIC(D) < 4.9406564584124654418E-324
               DISPLAY "D64 OK" ELSE DISPLAY "D64 BAD" END-IF
           IF FUNCTION SMALLEST-ALGEBRAIC(F) = 0.001
               DISPLAY "FIX OK" ELSE DISPLAY "FIX BAD" END-IF
           STOP RUN.
       END PROGRAM L1SMALGF.
