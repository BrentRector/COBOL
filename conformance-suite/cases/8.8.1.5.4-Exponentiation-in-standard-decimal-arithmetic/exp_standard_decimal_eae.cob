*>--- conformance-case
*> id: exp_standard_decimal_eae
*> rules: [GR-8.8.1.5.4-2, RV-15.4.1-3, RV-15.4.1-L2.1, RV-15.4.1-L2.2,
*>    FMT-15.27.2, FMT-15.35.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R18 (ledger F15/F19) - EXP / EXP10 / E / PI under ARITHMETIC IS STANDARD-DECIMAL.
      *> Derived from ISO/IEC 1989:2023:
      *>   15.4.1 r1   - under a standard mode "the returned value shall equal the value of the
      *>                 equivalent arithmetic expression" (NOTE 2: function = EAE is a TRUE relation).
      *>   15.34.4 r1  - EXP's EAE is (FUNCTION E ** (argument-1)).
      *>   15.35.4 r1  - EXP10's EAE is (10 ** (argument-1)); 8.8.1.5.4 r2a-d make an INTEGER power of
      *>                 an exact base EXACT (repeated SDIDI multiplication) - 10**23 is exactly 10^23,
      *>                 never binary64's 99999999999999991611392.
      *>   15.27.3 r3  - E is exactly  2.718281828459045235360287471352662 (34 digits); a fixed-point
      *>                 receiver caps at 31 digits (8.3.3.3.2), so the 9V9(30) stores below hold the
      *>                 constant TRUNCATED at 30 fraction digits (the no-ROUNDED default, 14.7.4).
      *>   15.73.3 r3  - PI is exactly 3.141592653589793238462643383279503 (34 digits).
      *>   15.4.1      - "the returned value is the same for all instances of a given function within a
      *>                 single execution": the two EXP10(0.5) receivers below hold prefixes of ONE SDIDI -
      *>                 before R18 they held two DIFFERENT quantizations.
      *>   15.84.4 r2  - since owner decision D-C (kb/Work PB167) the 8.8.1.5.4 r2e equivalent expression
      *>                 chosen at |operand-2| = 1/2 is FUNCTION SQRT(operand-1), whose standard-decimal
      *>                 value the standard fixes EXACTLY ("the exact square root of argument-1 rounded to
      *>                 34 digits"), so P17 below is the SPEC's sqrt(10) truncated at 17 fraction digits -
      *>                 3.16227766016837933, not the old binary64 development's ...950 (wrong from the
      *>                 17th significant digit). pb167_sdidi_exponentiation pins the identity itself.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R18EXPSD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E30  PIC 9V9(30).
       01 PI30 PIC 9V9(30).
       01 R25  PIC 9(25).
       01 P09  PIC 9V9(9).
       01 P17  PIC 9V9(17).
       PROCEDURE DIVISION.
           IF FUNCTION EXP(2) = FUNCTION E ** 2
             DISPLAY "EXP-EAE=EQ"
           ELSE
             DISPLAY "EXP-EAE=NE"
           END-IF.
           COMPUTE R25 = FUNCTION EXP10(23).
           DISPLAY "E23=" R25.
           COMPUTE E30 = FUNCTION E.
           DISPLAY "E=" E30.
           COMPUTE PI30 = FUNCTION PI.
           DISPLAY "PI=" PI30.
           COMPUTE P09 = FUNCTION EXP10(0.5).
           COMPUTE P17 = FUNCTION EXP10(0.5).
           DISPLAY "P09=" P09.
           DISPLAY "P17=" P17.
           STOP RUN.
