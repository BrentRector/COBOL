*>--- conformance-case
*> id: pb33_numval_family_digit_cap
*> rules: [AR-15.67.3-3, AR-15.68.3-6, AR-15.69.3-2, RV-15.93.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> THE NUMVAL-FAMILY DIGIT CAP BINDS THE VALUE PRODUCERS, NOT ONLY THE VALIDATORS.
      *> 15.67.3 rules 3-4 (NUMVAL), 15.68.3 rules 6-7 (NUMVAL-C) and 15.69.3 rules 2-3 (NUMVAL-F) cap
      *> argument-1 at 31 digits under native arithmetic and 34 under standard-decimal. NUMVAL-F caps the
      *> digits of the SIGNIFICAND, not of the whole literal.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE THE THREE VALIDATORS ENFORCED THE CAP AND THE THREE VALUE PRODUCERS DID
      *> NOT (fix-queue PB33 + PB34) - the same validating-twin-fixed asymmetry as PB32's MOD, three times over.
      *> MEASURED before the fix, with a 34-digit argument under >>TURN EC-ARGUMENT-FUNCTION CHECKING ON:
      *>     TEST-NUMVAL(34)   -> 32   TEST-NUMVAL-C(34) -> 32        (both correct)
      *>     NUMVAL(34), NUMVAL-C(34) -> 0141183460469231731687303715884
      *>     NUMVAL-F(34)             -> 7014118346046923173168730371588
      *> Those are Int128.MaxValue SATURATION ARTIFACTS - a plausible-looking 31-digit number, not a broken one -
      *> and EXECUTION CONTINUED PAST ALL THREE, so a FATAL exception condition was never set.
      *>
      *> This program runs with checking DISABLED, so 15.3's closing paragraph applies and an incorrect argument
      *> yields the implementor default 0. That is the observable that discriminates: 0 is the DEFAULT, whereas
      *> 0141183460469231731687303715884 was a value manufactured by saturation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB33NVCAP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> 31 digits - legal under every mode.
       01 S31 PIC X(44) VALUE "1234567890123456789012345.678901".
      *> 34 digits - legal under standard-decimal, too many under native.
       01 S34 PIC X(44) VALUE "1234567890123456789012345.678901234".
       01 C34 PIC X(44) VALUE "$1234567890123456789012345.678901234".
       01 F34 PIC X(44) VALUE "1234567890123456789012345678901234E+2".
       01 R   PIC S9(25)V9(6).
       01 V   PIC S9(9).
       PROCEDURE DIVISION.
      *> The 31-digit control: every producer returns the value.
           COMPUTE R = FUNCTION NUMVAL(S31)
           DISPLAY "NUMVAL-31=" R
      *> Over the native cap -> the 15.3 default 0, NOT a saturated value.
           COMPUTE R = FUNCTION NUMVAL(S34)
           DISPLAY "NUMVAL-34=" R
           COMPUTE R = FUNCTION NUMVAL-C(C34)
           DISPLAY "NUMVAL-C-34=" R
      *> NUMVAL-F counts the SIGNIFICAND only, so the E+2 exponent does not push it over on its own.
           COMPUTE R = FUNCTION NUMVAL-F(F34)
           DISPLAY "NUMVAL-F-34=" R
      *> ⚠ THE VALIDATORS MUST STILL AGREE AT THE SAME BOUNDARY - they were always right, and a fix that moved
      *> them would trade one asymmetry for another.
           COMPUTE V = FUNCTION TEST-NUMVAL(S31)
           DISPLAY "TEST-31=" V
           COMPUTE V = FUNCTION TEST-NUMVAL(S34)
           DISPLAY "TEST-34=" V
           COMPUTE V = FUNCTION TEST-NUMVAL-C(C34)
           DISPLAY "TEST-C-34=" V
           STOP RUN.
