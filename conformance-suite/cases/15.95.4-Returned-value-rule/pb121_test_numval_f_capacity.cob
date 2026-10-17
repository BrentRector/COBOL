*>--- conformance-case
*> id: pb121_test_numval_f_capacity
*> rules: [RV-15.95.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB121 — TEST-NUMVAL-F's ISO 15.95.4 r1 b) 6 capacity leg: "If standard-decimal arithmetic ...
      *> is in effect, and the magnitude of the numeric value in the argument exceeds the capacity of the
      *> standard intermediate data item used for that mode of arithmetic, the returned value is the position
      *> of the first digit of the exponent." The SDIDI range is +/-9.999...E+6144 (ISO 8.8.1.5.2 NOTE 2) and
      *> four exponent digits reach past it. Hand-derived: "1E+9999" -> the first exponent digit is position 4;
      *> "123E+6143" has most-significant-digit exponent 2+6143 = 6145 -> its first exponent digit is position
      *> 6; "1E+6144" and "-9.9E+6144" sit exactly at the cap and CONFORM; "1E-9999" is underflow, which does
      *> not "exceed the capacity" (the NUMVAL-F value twin's own 8.8.1.5.2 range check disposes of it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB121CP.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+9999")
           IF R = 4 DISPLAY "OVER OK" ELSE DISPLAY "OVER BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("123E+6143")
           IF R = 6 DISPLAY "MSD OK" ELSE DISPLAY "MSD BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+6144")
           IF R = 0 DISPLAY "EDGE OK" ELSE DISPLAY "EDGE BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("-9.9E+6144")
           IF R = 0 DISPLAY "NEG OK" ELSE DISPLAY "NEG BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E-9999")
           IF R = 0 DISPLAY "UNDR OK" ELSE DISPLAY "UNDR BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("--1")
           IF R = 2 DISPLAY "SIGN OK" ELSE DISPLAY "SIGN BAD " R END-IF
           STOP RUN.
