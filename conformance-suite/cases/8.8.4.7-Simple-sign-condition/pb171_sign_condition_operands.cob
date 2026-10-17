*>--- conformance-case
*> id: pb171_sign_condition_operands
*> rules: [SR-8.8.4.7.3-1, GR-8.8.4.7.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB171 - ISO 8.8.4.7.3 SR1 admits "any single numeric data item
      *> described with a usage other than a standard floating-point usage, or any
      *> form of arithmetic expression". These are the shapes that must SURVIVE
      *> the new NonNumericLiteralContext arm, and TWO OF THEM ARE CURRENTLY RIGHT
      *> BY ACCIDENT - which is precisely why they need a golden:
      *>   IF ZERO IS ZERO is TRUE only because the walk's drain returned 0;
      *>   8.8.1.1 names ZERO as the one admissible figurative, so it must stay
      *>   TRUE for the right reason after the fix.
      *> IF A - B IS ... pins the BREADTH-FIRST property (the operand is the WHOLE
      *> expression, SR1) that a depth-first leaf grab would break - the NC250A
      *> IF--TEST-55/56 fact, under its correct clause at last (the code cited
      *> 8.8.4.3, which is the SIMPLE BOOLEAN CONDITION).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB171SIGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC S9(4) VALUE 1.
       01 B  PIC S9(4) VALUE 5.
       01 N  PIC S9(4) VALUE -7.
       01 S  PIC S9V9  VALUE 0.5.
       01 FL USAGE FLOAT-SHORT.
       PROCEDURE DIVISION.
       MAIN.
           IF ZERO IS ZERO     DISPLAY "ZZ=T" ELSE DISPLAY "ZZ=F" END-IF
           IF ZERO IS POSITIVE DISPLAY "ZP=T" ELSE DISPLAY "ZP=F" END-IF
           IF ZERO IS NOT POSITIVE
                               DISPLAY "ZNP=T" ELSE DISPLAY "ZNP=F" END-IF
           IF A - B IS POSITIVE
                               DISPLAY "AB-P=T" ELSE DISPLAY "AB-P=F" END-IF
           IF A - B IS NEGATIVE
                               DISPLAY "AB-N=T" ELSE DISPLAY "AB-N=F" END-IF
           IF N IS NEGATIVE    DISPLAY "N=T" ELSE DISPLAY "N=F" END-IF
           IF S IS POSITIVE    DISPLAY "S=T" ELSE DISPLAY "S=F" END-IF
           MOVE -2 TO FL
           IF FL IS NEGATIVE   DISPLAY "FL=T" ELSE DISPLAY "FL=F" END-IF
           IF (FL) IS NEGATIVE DISPLAY "PFL=T" ELSE DISPLAY "PFL=F" END-IF
           STOP RUN.
