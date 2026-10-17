*>--- conformance-case
*> id: arithmetic_standard_decimal
*> rules: [GR-14.9.8.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.1.4 - ARITHMETIC IS STANDARD-DECIMAL (COBOL-2014, the OPTIONS paragraph): intermediate results
      *> use the standard DECIMAL intermediate data item, so 2 / 7 * 7 keeps full decimal precision = 2.00000
      *> (NOT the native form where 2/7 is clipped to the receiver scale before the * 7). Introduced 2014
      *> (COBOLNET0804 below 2014). Companion to options_paragraph (ARITHMETIC IS STANDARD) — P3 step 9 seed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ARITHSD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9(5).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "ASDOK".
           COMPUTE W = 2 / 7 * 7.
           DISPLAY "W=" W.
           STOP RUN.
