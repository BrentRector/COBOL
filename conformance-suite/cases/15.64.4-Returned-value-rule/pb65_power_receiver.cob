*>--- conformance-case
*> id: pb65_power_receiver
*> rules: [RV-15.64.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB65 / RV-15.64.4-1 — integer-operand ** is receiver-independent and exact to the carrier.
      *> §15.64.4 r1 (MOD's EAE) with argument-1 = A ** 2 (a legal §15.3 type-6 argument): the value is
      *> 999999999999999^2 mod 1000000007 = 13657001 (independent oracle), owed IDENTICALLY in every
      *> receiver and context — §15.4.1 native arithmetic approximates the EXPRESSION, and §15.2 5) makes
      *> MOD an integer function. The old receiver-bearing arm forced the FloatWorkingScale (≥9) landing
      *> onto the exact power, overflowed Int128 inside PowNativeInt, saturated, and MOD consumed the
      *> sentinel: 320612800 into S9(9)/S9(18)/S9(28) and the right answer into S9(31) — the receiver's
      *> PICTURE selected the value. Now: literal exponent → exact Int128 at scale 0; runtime-item or
      *> negative exponent → the SDIDI carrier (PowNativeIntDec) owning its scale at run time.
      *> REM (§15.77.4) rides the same argument, the reciprocal (§8.8.1.2) keeps its fraction on both
      *> arms, and 10 ** 30 stays exact (the PB18 case).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65POWRCV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A     PIC S9(18) VALUE 999999999999999.
       01 A13   PIC S9(18) VALUE 9999999999999.
       01 A14   PIC S9(18) VALUE 99999999999999.
       01 B     PIC S9(18) VALUE 1000000007.
       01 X     PIC S9(4)  VALUE 2.
       01 XN    PIC S9(4)  VALUE -2.
       01 R9    PIC S9(9)  SIGN LEADING SEPARATE.
       01 R18   PIC S9(18) SIGN LEADING SEPARATE.
       01 R28   PIC S9(28) SIGN LEADING SEPARATE.
       01 R31   PIC S9(31) SIGN LEADING SEPARATE.
       01 R4    PIC S9(9)V9(4) SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
           COMPUTE R9  = FUNCTION MOD(A ** 2, B)
             ON SIZE ERROR DISPLAY "SIZEERR-R9"
           END-COMPUTE
           DISPLAY "W09 =" R9
           COMPUTE R18 = FUNCTION MOD(A ** 2, B)
             ON SIZE ERROR DISPLAY "SIZEERR-R18"
           END-COMPUTE
           DISPLAY "W18 =" R18
           COMPUTE R28 = FUNCTION MOD(A ** 2, B)
             ON SIZE ERROR DISPLAY "SIZEERR-R28"
           END-COMPUTE
           DISPLAY "W28 =" R28
           COMPUTE R31 = FUNCTION MOD(A ** 2, B)
             ON SIZE ERROR DISPLAY "SIZEERR-R31"
           END-COMPUTE
           DISPLAY "W31 =" R31
           DISPLAY "DSP =" FUNCTION MOD(A ** 2, B)
           IF FUNCTION MOD(A ** 2, B) = 13657001
               DISPLAY "IF  =RIGHT"
           ELSE
               DISPLAY "IF  =WRONG"
           END-IF
           COMPUTE R18 = FUNCTION MOD(A13 ** 2, B)
           END-COMPUTE
           DISPLAY "B13 =" R18
           COMPUTE R18 = FUNCTION MOD(A14 ** 2, B)
           END-COMPUTE
           DISPLAY "B14 =" R18
           COMPUTE R18 = FUNCTION REM(A ** 2, B)
           END-COMPUTE
           DISPLAY "REM =" R18
           COMPUTE R18 = FUNCTION MOD(A ** X, B)
           END-COMPUTE
           DISPLAY "RTX =" R18
           COMPUTE R31 = A ** 2
           END-COMPUTE
           DISPLAY "POW =" R31
           COMPUTE R4 = 2 ** -2
           DISPLAY "NEGL=" R4
           COMPUTE R4 = 2 ** XN
           DISPLAY "NEGI=" R4
           COMPUTE R31 = 10 ** 30
           DISPLAY "T30 =" R31
           IF A ** X = A ** 2
               DISPLAY "EQAR=SAME"
           ELSE
               DISPLAY "EQAR=DIFFER"
           END-IF
           STOP RUN.
