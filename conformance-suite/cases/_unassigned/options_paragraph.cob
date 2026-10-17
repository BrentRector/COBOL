*>--- conformance-case
*> id: options_paragraph
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.1.2 / 8.8.1.4 - ARITHMETIC IS STANDARD performs operations in the standard intermediate
      *> data item, which for fixed-point (non-float) operands IS the standard DECIMAL form (8.8.1.5 SDIDI):
      *> 2 / 7 * 7 keeps full decimal precision = 2.00000, NOT the native result where 2/7 is clipped to the
      *> receiver scale before the * 7. Proves plain STANDARD routes to the CobolDec engine (DEVLOG 611, e).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPTTEST.
       OPTIONS.
           ARITHMETIC IS STANDARD
           DEFAULT ROUNDED MODE IS NEAREST-EVEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9(5).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "OPTOK".
           COMPUTE W = 2 / 7 * 7.
           DISPLAY "W=" W.
           STOP RUN.
