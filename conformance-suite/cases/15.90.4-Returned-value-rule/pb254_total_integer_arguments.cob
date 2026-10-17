*>--- conformance-case
*> id: pb254_total_integer_arguments
*> rules: [RV-15.90.4-1, RV-15.91.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB254 — ISO §15.90 TEST-DATE-YYYYMMDD / §15.91 TEST-DAY-YYYYDDD are TOTAL over the
      *> integers, and this program pins the half that had no coverage: r1a's UPPER bound, on every
      *> carrier an integer argument can arrive on, WITH EC-ARGUMENT-FUNCTION CHECKING ON.
      *>
      *> THE RULE. §15.90.3 r1 and §15.91.3 r1 are each one sentence — "Argument-1 shall be an
      *> integer" — and place NO constraint on the value. §15.90.4 r1a returns 1 "if the value of
      *> argument-1 is less than 16 010 000 or greater than 99 999 999" and §15.91.4 r1a returns 1
      *> outside 1 601 000..9 999 999; both are CATCH-ALLS, so every integer has a defined verdict.
      *> §15.3's closing paragraph sets EC-ARGUMENT-FUNCTION only when evaluating an argument
      *> "results in an incorrect value for that argument or for the returned value according to the
      *> rules specified in the function definition" — here there is no such value, so a conforming
      *> program may pass any integer and must get a verdict, never an abort.
      *>
      *> THE WITNESSES are all conforming: §8.3.3.3.2 requires fixed-point numeric literals of 1
      *> through 31 digits to be accepted, so T3's 19-digit literal is legal source; an integer data item of
      *> 19 and of 31 digits; the arithmetic-expression form (§15.3 argument type 6) that PB22
      *> documents reaching the intake intact.  The FLOATING-POINT carrier is deliberately NOT here:
      *> §15.3 type 6 admits "an integer data item or an always-integral arithmetic expression" and a
      *> floating-point item is neither, so kb/Work PB248 refuses it under strict (COBOLNET1627).  The float
      *> twins are reachable only under --permissive, and their r1a verdict is witnessed there, in
      *> FloatIntegerArgumentPermissiveTests (the home PB248 made for exactly this).
      *>
      *> ARMED, NOT ASSUMED. A checking-ON leg proves nothing if the directive is inert, so ARMED
      *> deliberately raises the condition from a function whose argument rules DO bound the value —
      *> §15.5.2's integer date form, where an out-of-range argument really is an incorrect value —
      *> and the declarative reports it. Every leg after it runs under the same armed directive.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB254TOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2    PIC 9(2).
       01 BIG19 PIC 9(19) VALUE 9999999999999999999.
       01 B31   PIC 9(31) VALUE
           1234567890123456789012345678901.
       01 P18   PIC 9(18) VALUE 184467440737115466.
       01 ES    PIC X(20).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           MOVE FUNCTION EXCEPTION-STATUS TO ES
           DISPLAY "ARMED=" ES.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> ARMED — INTEGER-OF-DAY's argument-1 must be in §15.5.2 integer date form, so BIG19 IS an
      *> incorrect value and §15.3 sets the condition; the aborted COMPUTE stores nothing, so R2
      *> still shows the 99 preset. This is the control: if the directive were inert the declarative
      *> would not report and A0 would not be 99.
           MOVE 99 TO R2
           COMPUTE R2 = FUNCTION INTEGER-OF-DAY(BIG19)
           DISPLAY "A0=" R2
      *> ── §15.90.4 r1a, the UPPER half ──────────────────────────────────────────────
      *> T1: 100 000 000 is greater than 99 999 999 → 1.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(100000000)
           DISPLAY "T1=" R2
      *> T2: the boundary is EXCLUSIVE — 99 999 999 is NOT greater than 99 999 999, so r1a does not
      *> fire and the chain falls to r1b: MOD(99999999 10000) = 9999, greater than 1299 → 2.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(99999999)
           DISPLAY "T2=" R2
      *> T3: a 19-digit LITERAL, above 9 223 372 036 854 775 807 → r1a → 1.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(9999999999999999999)
           DISPLAY "T3=" R2
      *> T4/T5: the same value as a 19-digit and as a 31-digit DATA ITEM → 1.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(BIG19)
           DISPLAY "T4=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(B31)
           DISPLAY "T5=" R2
      *> T6: the §15.3 type-6 arithmetic-expression witness — 184467440737115466 * 100 + 62 is
      *> 18 446 744 073 711 546 662 = 2**64 + 1 995 046, PB22's own documented shape → 1.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(P18 * 100 + 62)
           DISPLAY "T6=" R2
      *> ── §15.91.4 r1a, the UPPER half ──────────────────────────────────────────────
      *> U1: 10 000 000 is greater than 9 999 999 → 1.
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(10000000)
           DISPLAY "U1=" R2
      *> U2: the boundary is EXCLUSIVE — 9 999 999 passes r1a, then r1b: MOD(9999999 1000) = 999 and
      *> year 9999 is not divisible by 4, so the year has 365 days and 999 exceeds it → 2.
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(9999999)
           DISPLAY "U2=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(9999999999999999999)
           DISPLAY "U3=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(BIG19)
           DISPLAY "U4=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(B31)
           DISPLAY "U5=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(P18 * 100 + 62)
           DISPLAY "U6=" R2
           STOP RUN.
       END PROGRAM PB254TOT.
