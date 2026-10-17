*>--- conformance-case
*> id: pb56_dec_carrier_intrinsics
*> rules: [RV-15.4.1-L2.1, RV-15.8.4-1, RV-15.9.4-1, RV-15.10.4-1, RV-15.11.4-1, RV-15.20.4-1, RV-15.34.4-1, RV-15.55.4-1, RV-15.56.4-1, RV-15.62.4-1, RV-15.74.4-1, RV-15.81.4-1, RV-15.98.4-1, FMT-15.86.2, FMT-15.98.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.4.1 r1 under ARITHMETIC IS STANDARD-DECIMAL — the Dec-carrier intrinsic bodies
      *> (fix-queue PB56). "The returned value shall equal the value of the equivalent arithmetic
      *> expression": every line below derives from the function's own EAE evaluated on the SDIDI
      *> (§8.8.1.5.2), with NO argument quantization — the interim landing truncated Dec operands at
      *> working scale 6, so SIGN(A9 − 0) of a 1e-9 operand returned 0 where §15.81.4 r1a says +1.
      *> Derivations: SIGN(1e-9)=+1 · MAX(1e-9, 0)=1e-9 · MOD(1e-9·10^40, 7)=3 (§15.64.3 r1
      *> integers; §15.64.4 r1: 10^31 − 7·INTEGER(10^31/7) = 3; quantized at scale 9 it needs 40 digits, past Int128) · MEDIAN(2e-9 4e-9 9e-9)=4e-9 · VARIANCE(1 2 3)=2/3
      *> (VARIANCE, §15.98.4: ((1−2)²+0+1)/3), stored truncating into 9V9(5) → 0.66666 · ANNUITY(0, 4)=1/4
      *> (§15.9.4 r1 rate-zero arm) · PRESENT-VALUE(1, 8 8)=8/2+8/4=6 (§15.74.4 r1) ·
      *> STANDARD-DEVIATION(2 2 2)=√0=0. The four financial/statistical functions were COBOLNET0899-
      *> staged under the standard modes until these SDIDI evaluations existed; this golden is the
      *> unstaging's proof, and the former negative fixture arith-standard-intrinsic-staged.cob
      *> retired with the stage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB56DEC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A9  PIC S9V9(9) VALUE 0.000000001.
       01 SG  PIC +9.
       01 E9  PIC 9.9(9).
       01 EM  PIC 9.9(9).
       01 ED  PIC 9.9(9).
       01 VA  PIC 9.9(5).
       01 AN  PIC 9.99.
       01 PV  PIC 9.
       01 SDV PIC 9.9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE SG = FUNCTION SIGN(A9 - 0)
           DISPLAY "SG=" SG
           COMPUTE E9 = FUNCTION MAX(A9 - 0, 0)
           DISPLAY "MX=" E9
           COMPUTE EM = FUNCTION MOD(A9 * 10 ** 40, 7)
           DISPLAY "MO=" EM
           COMPUTE ED = FUNCTION MEDIAN(A9 * 2, A9 * 4, A9 * 9)
           DISPLAY "ME=" ED
           COMPUTE VA = FUNCTION VARIANCE(1, 2, 3)
           DISPLAY "VA=" VA
           COMPUTE AN = FUNCTION ANNUITY(0, 4)
           DISPLAY "AN=" AN
           COMPUTE PV = FUNCTION PRESENT-VALUE(1, 8, 8)
           DISPLAY "PV=" PV
           COMPUTE SDV = FUNCTION STANDARD-DEVIATION(2, 2, 2)
           DISPLAY "SD=" SDV
      *> The exponent lifts from the RAW operand (e^1e-9 = 1.000000001…, §15.34.4 r1's EAE over the
      *> exact §15.27.3 r3 E), and a prose-family result converts in per §8.8.1.5.1 instead of
      *> quantizing (SQRT(4e-18) ≈ 2e-9) — the two sibling arms of the same landing defect.
           COMPUTE E9 = FUNCTION EXP(A9 - 0)
           DISPLAY "EX=" E9
           COMPUTE E9 = FUNCTION SQRT(A9 * A9 * 4)
           DISPLAY "SQ=" E9
      *> The prose family's exact-valued points are exact through the conversion (ACOS(1)=0, LOG(1)=0,
      *> LOG10(100)=2, COS(0)=1) and its approximations land through ONE final transfer at the
      *> receiver's mode — ASIN(1)=π/2 and ATAN(1)=π/4 truncate at scale 9 (…326, …163), where the
      *> old double-quantization rounded twice (…327).
           COMPUTE E9 = FUNCTION ACOS(1)
           DISPLAY "AC=" E9
           COMPUTE E9 = FUNCTION LOG(1)
           DISPLAY "LG=" E9
           COMPUTE E9 = FUNCTION LOG10(100)
           DISPLAY "L1=" E9
           COMPUTE E9 = FUNCTION ASIN(1)
           DISPLAY "AS=" E9
           COMPUTE E9 = FUNCTION ATAN(1)
           DISPLAY "AT=" E9
           COMPUTE E9 = FUNCTION COS(0)
           DISPLAY "CO=" E9
           STOP RUN.
