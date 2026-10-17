*>--- conformance-case
*> id: pb121_test_numval_f_positions
*> rules: [RV-15.95.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB121 — TEST-NUMVAL-F's ISO 15.95.4 r1 b)/c) dispatch under NATIVE arithmetic. Leg b: "if one
      *> or more characters are in error, the position of the first character in error" — a scan that broke on
      *> a real character reports THAT character even when no significand digit was scanned ("--1" -> 2: the
      *> second sign; "ABC" -> 1). Leg c ("Otherwise" - LENGTH+1) covers only no-character-in-error shapes; the
      *> defect returned LENGTH+1 for both. And r1 b) 6's capacity leg names ONLY the standard arithmetic
      *> modes, so under native arithmetic "1E+9999" CONFORMS (0) — pinning the mode gate from the native side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB121NP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F("--1")
           IF R = 2 DISPLAY "SIGN OK" ELSE DISPLAY "SIGN BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("ABC")
           IF R = 1 DISPLAY "ALPH OK" ELSE DISPLAY "ALPH BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("+.A")
           IF R = 3 DISPLAY "INCH OK" ELSE DISPLAY "INCH BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(" +.")
           IF R = 4 DISPLAY "INCO OK" ELSE DISPLAY "INCO BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+9999")
           IF R = 0 DISPLAY "NCAP OK" ELSE DISPLAY "NCAP BAD " R END-IF
           STOP RUN.
