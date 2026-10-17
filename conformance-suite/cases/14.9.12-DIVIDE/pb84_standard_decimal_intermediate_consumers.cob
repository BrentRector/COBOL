*>--- conformance-case
*> id: pb84_standard_decimal_intermediate_consumers
*> rules: [GR-14.9.12.4-1, GR-14.9.12.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB84 — the STANDARD-DECIMAL twin. Under ARITHMETIC IS
      *> STANDARD-DECIMAL every arithmetic expression is an SDIDI intermediate
      *> (§8.8.1.5.2), so the same consumers — the sign condition (§8.8.4.7),
      *> SET pointer UP/DOWN BY (§14.9.39 Format 10), ALLOCATE … CHARACTERS
      *> (§14.9.3), CALL … BY VALUE (§14.9.4), INVOKE … BY CONTENT (§14.9.23),
      *> and DIVIDE … REMAINDER over a float sender (§14.9.12.4 GR7, PB85) —
      *> were Roslyn errors on every expression here before the ONE landing.
      *> Expected values are exact: 12 × 5 − 60 = 0; 12 / 5 = 2.4 > 0; 5 − 12
      *> < 0; 12 / 8 − 1.5 = 0 exactly on the SDIDI; 12 / 8 = 1.5; 7.5 / 2 →
      *> quotient 3, remainder 1.5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB84STDDEC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB84SDX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O   USAGE OBJECT REFERENCE CPB84SDX.
       01 A   PIC 9(4) VALUE 12.
       01 B   PIC 9(4) VALUE 5.
       01 F   USAGE FLOAT-LONG VALUE 7.5.
       01 Q   PIC 9(4).
       01 R   PIC 9(4)V99.
       01 P   USAGE POINTER.
       01 P2  USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           IF A * B - 60 IS ZERO DISPLAY "S1 ZERO"
              ELSE DISPLAY "S1 WRONG" END-IF.
           IF A / B IS POSITIVE DISPLAY "S2 POSITIVE"
              ELSE DISPLAY "S2 WRONG" END-IF.
           IF B - A IS NEGATIVE DISPLAY "S3 NEGATIVE"
              ELSE DISPLAY "S3 WRONG" END-IF.
           IF A / 8 - 1.5 IS ZERO DISPLAY "S4 ZERO"
              ELSE DISPLAY "S4 WRONG" END-IF.
           IF A / 8 - 1.5 IS NOT POSITIVE DISPLAY "S5 NOT POSITIVE"
              ELSE DISPLAY "S5 WRONG" END-IF.
           ALLOCATE 10 CHARACTERS RETURNING P.
           SET P2 TO P.
           SET P UP BY A + 1.
           IF P NOT = P2 DISPLAY "S6 MOVED" ELSE DISPLAY "S6 WRONG" END-IF.
           SET P DOWN BY A + 1.
           IF P = P2 DISPLAY "S6 BACK" ELSE DISPLAY "S6 WRONG" END-IF.
           FREE P2.
           ALLOCATE A * 2 CHARACTERS RETURNING P.
           IF P NOT = NULL DISPLAY "S7 ALLOCATED"
              ELSE DISPLAY "S7 WRONG" END-IF.
           FREE P.
           CALL "PB84SDSUB" AS NESTED USING BY VALUE A / 8.
           CALL "PB84SDSUB" AS NESTED USING BY VALUE (A * B + 1).
           INVOKE CPB84SDX "NEW" RETURNING O.
           INVOKE O "TAKEN" USING BY CONTENT A * B + 1.
           INVOKE O "TAKEF" USING BY CONTENT A / 8.
           DIVIDE F BY 2 GIVING Q REMAINDER R.
           DISPLAY "S10 Q=" Q " R=" R.
           DIVIDE A BY B GIVING Q REMAINDER R.
           DISPLAY "S11 Q=" Q " R=" R.
           STOP RUN.

      *> kb/Work PB131 - AS NESTED requires CONTAINMENT (§14.9.4.3 SR15 sentence 2, enforced at bind).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB84SDSUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V PIC 9(4)V9(4).
       PROCEDURE DIVISION USING BY VALUE V.
           DISPLAY "SUB V=" V.
           GOBACK.
       END PROGRAM PB84SDSUB.
       END PROGRAM PB84STDDEC.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB84SDX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9(6).
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "N=[" P "]".
       END METHOD TAKEN.
       METHOD-ID. TAKEF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 9V99.
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "F=[" P "]".
       END METHOD TAKEF.
       END OBJECT.
       END CLASS CPB84SDX.
