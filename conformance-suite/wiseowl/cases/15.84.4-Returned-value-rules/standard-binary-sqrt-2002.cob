*>--- conformance-case
*> id: standard-binary-sqrt-2002
*> rules: [RV-15.84.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> The OTHER leg of the same clause, asserted separately: at --std 2002 the mode does
      *> not EXIST yet (STANDARD-BINARY was introduced by COBOL-2014), so the introduction
      *> gate COBOLNET0900 is what must fire -- not the 4.2.6 non-support screen. Two
      *> different obligations; collapsing them would let a regression in either hide
      *> behind the other.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SBSQRT2002.
       OPTIONS.
           ARITHMETIC IS STANDARD-BINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R PIC 9(4)V9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION SQRT(2).
           DISPLAY R.
           STOP RUN.
