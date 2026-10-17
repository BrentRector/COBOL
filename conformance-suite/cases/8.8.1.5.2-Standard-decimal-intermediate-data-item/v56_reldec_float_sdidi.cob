*>--- conformance-case
*> id: v56_reldec_float_sdidi
*> rules: [GR-8.8.1.5.2-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> V56 (CONFORMANCE-FIX-QUEUE): under OPTIONS ARITHMETIC IS STANDARD-DECIMAL a numeric comparison converts
      *> EACH operand to a standard-decimal intermediate (SDIDI, decimal128) and compares decimally (ISO §8.8.4.2.4:
      *> "the comparison is performed as if each operand ... had been converted to that form"). A float lifts via the
      *> §8.8.1.5.1 float->SDIDI conversion; a fixed operand lifts EXACTLY, preserving decimal precision beyond
      *> binary64. F=1.0 (COMP-2) vs D=1.00000000000000001 (18 exact significant digits) compare UNEQUAL (NE).
      *> Pre-fix the relational path took the native IEEE branch unconditionally, rounding the 18-digit D to double
      *> (1.0), so `F = D` wrongly compared 1.0 == 1.0 and printed EQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. V56.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE COMP-2.
       01 D PIC 9V9(17) VALUE 1.00000000000000001.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO F
           IF F = D
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           STOP RUN.
