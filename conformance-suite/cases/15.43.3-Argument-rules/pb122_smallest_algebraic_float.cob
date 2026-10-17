*>--- conformance-case
*> id: pb122_smallest_algebraic_float
*> rules: [AR-15.43.3-2, AR-15.83.3-4, RV-15.83.4-1, RV-15.83.4-2, DOC-A.1-180]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB122 — SMALLEST-ALGEBRAIC of a native floating-point item under STANDARD-DECIMAL
      *> arithmetic. ISO 15.83.3 r2 bars only the STANDARD BINARY usages (3.166: FLOAT-BINARY-*) under this
      *> mode; a native COMP-1/COMP-2 argument-1 is LEGAL source, and the old blanket refusal (licensed only
      *> by r4's NATIVE latitude) was an over-rejection here. 15.83.1/15.83.4 r2: the value is the smallest
      *> algebraic difference between two values the item can represent — the carrier's smallest positive
      *> subnormal (binary64 2^-1074 ~ 4.94E-324; binary32 2^-149 ~ 1.4013E-45), folded at full decimal128
      *> precision. The comparisons run in decimal128 (standard-decimal mode), so the bounds pin the fold's
      *> actual magnitude; 15.83.4 r1's IN-ARITHMETIC-RANGE screen passes (both extremes lie inside the
      *> SDIDI's range). HIGHEST/LOWEST on the same items guard the R10 arms beside the new one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB122SF.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE COMP-2.
       01 S USAGE COMP-1.
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION SMALLEST-ALGEBRAIC(D) > 0
              AND FUNCTION SMALLEST-ALGEBRAIC(D) < 5.0E-324
               DISPLAY "D64 OK" ELSE DISPLAY "D64 BAD" END-IF
           IF FUNCTION SMALLEST-ALGEBRAIC(S) > 1.4E-45
              AND FUNCTION SMALLEST-ALGEBRAIC(S) < 1.41E-45
               DISPLAY "S32 OK" ELSE DISPLAY "S32 BAD" END-IF
           IF FUNCTION HIGHEST-ALGEBRAIC(S) > 3.4E+38
              AND FUNCTION HIGHEST-ALGEBRAIC(S) < 3.5E+38
               DISPLAY "HI OK" ELSE DISPLAY "HI BAD" END-IF
           IF FUNCTION LOWEST-ALGEBRAIC(D) < -1.79E+308
               DISPLAY "LO OK" ELSE DISPLAY "LO BAD" END-IF
           STOP RUN.
