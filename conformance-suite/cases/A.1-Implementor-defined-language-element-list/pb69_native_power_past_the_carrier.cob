*>--- conformance-case
*> id: pb69_native_power_past_the_carrier
*> rules: [DOC-A.1-179]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB69 - a native integer power that leaves the Int128 carrier consumed as a VALUE (an argument, a
      *> comparison operand, an arithmetic operand) - and the exact fast path it must not cost. 8.8.1.3 makes
      *> native arithmetic implementor-defined; COBOL.NET's determination (owner decision 2026-08-03; the
      *> numeric design D3): an integer power is EXACT while it fits the Int128 carrier and the double
      *> approximation past it, and (PB69) that value rides the SDIDI carrier - one arm for a literal and a
      *> data-item exponent, no saturated sentinel anywhere. 15.4.1: under native arithmetic a function's
      *> returned value is "an implementor-defined approximation of the value of that expression" - a
      *> function of the OPERAND, which the Int128.MaxValue sentinel was not (A**3 and A**4 answered the SAME
      *> MOD).
      *> A = 999999999999999 (15 digits), B = 1000000007.
      *> T2/T4/T11: A**2 (30 digits) fits, exact: MOD = 13657001 for the literal AND the data-item exponent
      *>   (they gave 13657001 / 13657001 before too), MOD(A**2 * 3, B) = 40971003 (the product on the SDIDI is
      *>   exact at 31 digits, ModDec's integer fast path is exact).
      *> T5/T6: A**4 > A**3 is TRUE and A**3 = A**4 is FALSE - two Decs of different magnitude (before PB69 both
      *>   saturated to the same sentinel: FALSE / TRUE).
      *> T7: COMPUTE R31 = A**2 exact; T8: A**3 (45 digits) into a 31-digit receiver is a SIZE ERROR (the checked
      *>   store); T9: A**3 - A**3 = 0 on the SDIDI; T10: A**3 / A**2 = 999999999999998 (the approximation's
      *>   ratio - before PB69 the sentinel gave 170141183; before the PB83 division fix the SDIDI gave 999).
      *> T1/T3: MOD(A**3, B) - the SAME value for both spellings (before PB69: 639816141 from the sentinel and
      *>   966729007 from the Dec arm's modular landing). The value is the SDIDI equivalent arithmetic expression
      *>   over the 15-significant-digit approximation of A**3, whose residue mod B is below the approximation's
      *>   granularity - 0 - an approximation of the expression, consistent, never a sentinel. (The exact
      *>   980012199 needs 45-digit arithmetic no native mode of this compiler has; the value is documented under
      *>   A.1 item 179 and the ** determination in CONFORMANCE.md.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB69NATIVEPOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(18) VALUE 999999999999999.
       01 B PIC S9(18) VALUE 1000000007.
       01 X PIC S9(4) VALUE 3.
       01 Y PIC S9(4) VALUE 2.
       01 R18 PIC S9(18).
       01 R31 PIC S9(31).
       PROCEDURE DIVISION.
           COMPUTE R18 = FUNCTION MOD(A ** 3, B)  DISPLAY "T1 MOD(A**3,B)=" R18
           COMPUTE R18 = FUNCTION MOD(A ** 2, B)  DISPLAY "T2 MOD(A**2,B)=" R18
           COMPUTE R18 = FUNCTION MOD(A ** X, B)  DISPLAY "T3 MOD(A**X,B)=" R18
           COMPUTE R18 = FUNCTION MOD(A ** Y, B)  DISPLAY "T4 MOD(A**Y,B)=" R18
           IF A ** 4 > A ** 3 DISPLAY "T5 A**4>A**3: TRUE" ELSE DISPLAY "T5 A**4>A**3: FALSE" END-IF
           IF A ** 3 = A ** 4 DISPLAY "T6 A**3=A**4: TRUE" ELSE DISPLAY "T6 A**3=A**4: FALSE" END-IF
           COMPUTE R31 = A ** 2 DISPLAY "T7 A**2=" R31
           COMPUTE R31 = A ** 3 ON SIZE ERROR DISPLAY "T8 A**3 SIZE ERROR" NOT ON SIZE ERROR DISPLAY "T8 A**3=" R31 END-COMPUTE
           COMPUTE R31 = A ** 3 - A ** 3 ON SIZE ERROR DISPLAY "T9 SIZE ERROR" NOT ON SIZE ERROR DISPLAY "T9 A**3-A**3=" R31 END-COMPUTE
           COMPUTE R31 = A ** 3 / A ** 2 ON SIZE ERROR DISPLAY "T10 SIZE ERROR" NOT ON SIZE ERROR DISPLAY "T10 A**3/A**2=" R31 END-COMPUTE
           COMPUTE R18 = FUNCTION MOD(A ** 2 * 3, B) DISPLAY "T11 MOD(A**2*3,B)=" R18
           STOP RUN.
       END PROGRAM PB69NATIVEPOW.
