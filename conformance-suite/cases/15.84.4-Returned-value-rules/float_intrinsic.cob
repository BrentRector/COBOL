*>--- conformance-case
*> id: float_intrinsic
*> rules: [RV-15.84.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTINT.
      *> a transcendental intrinsic into a FLOAT receiver keeps full
      *> binary64 precision (not the scale-9 fixed quantize). D16 review.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION SQRT(2).
           DISPLAY "SQRT=" WS-R.
           COMPUTE WS-R = 10 / 3.
           DISPLAY "DIV=" WS-R.
           STOP RUN.
