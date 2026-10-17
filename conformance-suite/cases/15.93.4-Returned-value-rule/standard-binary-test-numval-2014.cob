*>--- conformance-case
*> id: standard-binary-test-numval-2014
*> rules: [RV-15.93.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> D-A (owner protocol 2026-08-30): ARITHMETIC IS STANDARD-BINARY is DECLINED.
      *> Annex A.3 item 2 makes the clause processor-dependent, and ISO 4.2.6 gives the
      *> implementor the discretion not to claim support plus the duty to warn at compile
      *> time and to document the absence. COBOL.NET's warning mechanism is the hard
      *> COBOLNET0806 (docs/CONFORMANCE.md 1 explains why an error rather than a warning).
      *> This case NAMES the function whose returned value 15.x defines in terms of the
      *> arithmetic mode, so the decline is proven on the path the inventory row is about
      *> and not merely on the clause in isolation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SBTNV2014.
       OPTIONS.
           ARITHMETIC IS STANDARD-BINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R PIC 9(4)V9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL("12").
           DISPLAY R.
           STOP RUN.
