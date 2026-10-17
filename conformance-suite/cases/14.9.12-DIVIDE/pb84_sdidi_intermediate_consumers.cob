*>--- conformance-case
*> id: pb84_sdidi_intermediate_consumers
*> rules: [GR-14.9.12.4-6, GR-14.9.12.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB84 + PB85. Under NATIVE arithmetic an integer power is an
      *> SDIDI intermediate (PB69 — one `**` arm, Dec-carried), and every
      *> consumer of a rendered intermediate that had only a native arm was a
      *> Roslyn error on conforming COBOL: the sign condition (§8.8.4.7 —
      *> `IF 9 ** TWO + (180 - 90) IS NOT POSITIVE`, NIST NC250A), SET
      *> pointer UP/DOWN BY (SET, §14.9.39 Format 10), ALLOCATE … CHARACTERS
      *> (§14.9.3), CALL … BY VALUE arithmetic-expression (§14.9.4), and
      *> INVOKE … BY CONTENT arithmetic-expression (§14.9.23). PB85: DIVIDE
      *> … REMAINDER (§14.9.12.4 GR7) over a FLOAT-LONG sender snapshotted
      *> the double into an Int128 — CS0266. Expected values are the algebraic
      *> results: 12² = 144; 9⁵ = 59049; 2⁻² + 1 = 1.25; 7.5 / 2 → quotient 3
      *> (§14.9.12.4 GR6c truncates at the receiver's scale), remainder
      *> 7.5 − 3 × 2 = 1.5 (GR7); 17 / 7.5 → 2, remainder 17 − 2 × 7.5 = 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB84NATIVE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB84X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O   USAGE OBJECT REFERENCE CPB84X.
       01 A   PIC 9(4) VALUE 12.
       01 B   PIC 9(4) VALUE 5.
       01 F   USAGE FLOAT-LONG VALUE 7.5.
       01 Q   PIC 9(4).
       01 R   PIC 9(4)V99.
       01 P   USAGE POINTER.
       01 P2  USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
      *> §8.8.4.7 sign conditions over a native integer power.
           IF A ** 2 IS POSITIVE DISPLAY "T1 POSITIVE"
              ELSE DISPLAY "T1 WRONG" END-IF.
           IF A ** 2 - 200 IS NEGATIVE DISPLAY "T2 NEGATIVE"
              ELSE DISPLAY "T2 WRONG" END-IF.
           IF A ** 2 - 144 IS ZERO DISPLAY "T3 ZERO"
              ELSE DISPLAY "T3 WRONG" END-IF.
           IF A ** 2 - 144 IS NOT ZERO DISPLAY "T4 WRONG"
              ELSE DISPLAY "T4 NOT-ZERO IS FALSE" END-IF.
           IF 9 ** B - 59049 IS ZERO DISPLAY "T5 ZERO"
              ELSE DISPLAY "T5 WRONG" END-IF.
           IF 0 - 9999 ** 3 IS NEGATIVE DISPLAY "T6 NEGATIVE"
              ELSE DISPLAY "T6 WRONG" END-IF.
           IF A ** 2 IS POSITIVE AND B ** 2 - 25 IS ZERO
              DISPLAY "T7 COMPOUND" ELSE DISPLAY "T7 WRONG" END-IF.
      *> §14.9.39 Format 10 — SET pointer UP BY / DOWN BY an SDIDI amount.
           ALLOCATE 10 CHARACTERS RETURNING P.
           SET P2 TO P.
           SET P UP BY A ** 2.
           IF P NOT = P2 DISPLAY "T8 MOVED" ELSE DISPLAY "T8 WRONG" END-IF.
           SET P DOWN BY A ** 2.
           IF P = P2 DISPLAY "T8 BACK" ELSE DISPLAY "T8 WRONG" END-IF.
           FREE P2.
      *> §14.9.3 — ALLOCATE arithmetic-expression CHARACTERS.
           ALLOCATE A ** 2 CHARACTERS RETURNING P.
           IF P NOT = NULL DISPLAY "T9 ALLOCATED"
              ELSE DISPLAY "T9 WRONG" END-IF.
           FREE P.
      *> §14.9.4 — CALL … BY VALUE arithmetic-expression.
           CALL "PB84SUB" AS NESTED USING BY VALUE A ** 2.
           CALL "PB84SUB" AS NESTED USING BY VALUE (2 ** -2 + 1).
      *> §14.9.23 — INVOKE … BY CONTENT arithmetic-expression.
           INVOKE CPB84X "NEW" RETURNING O.
           INVOKE O "TAKEN" USING BY CONTENT A ** 2.
           INVOKE O "TAKEF" USING BY CONTENT 2 ** -2 + 1.
      *> §14.9.12.4 GR6c/GR7 — DIVIDE … REMAINDER over a float sender (PB85).
           DIVIDE F BY 2 GIVING Q REMAINDER R.
           DISPLAY "T12 Q=" Q " R=" R.
           DIVIDE 17 BY F GIVING Q REMAINDER R.
           DISPLAY "T13 Q=" Q " R=" R.
           DIVIDE A BY B GIVING Q REMAINDER R.
           DISPLAY "T14 Q=" Q " R=" R.
           STOP RUN.

      *> kb/Work PB131 - AS NESTED requires CONTAINMENT (§14.9.4.3 SR15 sentence 2, enforced at bind).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB84SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V PIC 9(4)V9(4).
       PROCEDURE DIVISION USING BY VALUE V.
           DISPLAY "SUB V=" V.
           GOBACK.
       END PROGRAM PB84SUB.
       END PROGRAM PB84NATIVE.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB84X INHERITS FROM BASE.
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
       END CLASS CPB84X.
