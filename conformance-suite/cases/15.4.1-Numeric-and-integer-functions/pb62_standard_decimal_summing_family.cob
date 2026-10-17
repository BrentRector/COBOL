*>--- conformance-case
*> id: pb62_standard_decimal_summing_family
*> rules: [RV-15.4.1-L2.1, RV-15.60.4-1, RV-15.88.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB62 (RV-15.60.4-1) - the SUMMING statistical family under ARITHMETIC IS STANDARD-DECIMAL evaluates
      *> its equivalent arithmetic expression on the SDIDI carrier, each argument converted INDIVIDUALLY. 15.4.1:
      *> under a standard mode "when an equivalent arithmetic expression is specified ... the returned value shall
      *> equal the value of the equivalent arithmetic expression"; 8.8.1.5.2 r1 converts every fixed-point operand
      *> into an SDIDI exactly (34 digits, NOTE 2). Before PB62 the native arms first ALIGNED the arguments to
      *> the list's maximum scale on the Int128 carrier - a 31-digit integer beside a scale-18 item needs 49
      *> digits - so the alignment raised a size error where the SDIDI holds every answer below exactly.
      *> MEAN  15.60.4 r1b: ((A + B) / 2) = (10^30 + 2) / 2 = 500000000000000000000000000001
      *> SUM   15.88.4:     10^30 + 2     = 1000000000000000000000000000002
      *> MIDR  15.62.4:     (max + min)/2 = 500000000000000000000000000001
      *> RANGE 15.76.4:     max - min     = 999999999999999999999999999998
      *> MEDIAN 15.61.4 r1: the middle of (2.0, 2.0, 10^30) sorted = 2.0
      *> Each receiver is pre-set to 7 so a size-error no-op cannot be misread as a value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB62SDSUMMING.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(31) VALUE 1000000000000000000000000000000.
       01 WS-B PIC 9V9(18) VALUE 2.0.
       01 R1 PIC 9(31) VALUE 7.
       01 R2 PIC 9(31) VALUE 7.
       01 R3 PIC 9(31) VALUE 7.
       01 R4 PIC 9(31) VALUE 7.
       01 R5 PIC 9(30)V9 VALUE 7.
       PROCEDURE DIVISION.
           COMPUTE R1 = FUNCTION MEAN(WS-A WS-B)
             ON SIZE ERROR DISPLAY "MEAN-SIZEERR" END-COMPUTE
           DISPLAY "MEAN=" R1
           COMPUTE R2 = FUNCTION SUM(WS-A WS-B)
             ON SIZE ERROR DISPLAY "SUM-SIZEERR" END-COMPUTE
           DISPLAY "SUM=" R2
           COMPUTE R3 = FUNCTION MIDRANGE(WS-A WS-B)
             ON SIZE ERROR DISPLAY "MIDR-SIZEERR" END-COMPUTE
           DISPLAY "MIDR=" R3
           COMPUTE R4 = FUNCTION RANGE(WS-A WS-B)
             ON SIZE ERROR DISPLAY "RANGE-SIZEERR" END-COMPUTE
           DISPLAY "RANGE=" R4
           COMPUTE R5 = FUNCTION MEDIAN(WS-A WS-B WS-B)
             ON SIZE ERROR DISPLAY "MEDIAN-SIZEERR" END-COMPUTE
           DISPLAY "MEDIAN=" R5
           STOP RUN.
       END PROGRAM PB62SDSUMMING.
