*>--- conformance-case
*> id: pb1041-exp-before-2002
*> rules: [FMT-15.34.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.34 - the EXP function is introduced by ISO/IEC 1989:2002; the COBOL-85
      *> Intrinsic Function Module has no EXP, so an exact 18-digit argument whose 2002
      *> value the positive golden 2002/pb1041_exp_and_wide_exact_argument pins is refused
      *> here by the edition gate (COBOLNET1502), not silently evaluated. Witness for kb/Work PB1041.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1041EXP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(3)V9(15) VALUE 700.123456789012345.
       01 R PIC 9V9(14).
       PROCEDURE DIVISION.
           COMPUTE R ROUNDED = FUNCTION EXP(X) / 1000
           STOP RUN.
