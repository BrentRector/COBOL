*>--- conformance-case
*> id: pb636_random_total_seed
*> rules: [AR-15.75.3-2, RV-15.75.4-1, RV-15.75.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB636 — ISO §15.75 RANDOM's SEED is a TOTAL argument, and this program pins the half
      *> that had no coverage: a seed past the 64-bit window, WITH EC-ARGUMENT-FUNCTION CHECKING ON.
      *>
      *> THE RULE. §15.75.3 r2 is one sentence about the SIGN — "If argument-1 is specified, it shall
      *> be zero or a positive integer. It is used as the seed value to generate a sequence of
      *> pseudo-random numbers" — and places no constraint on the MAGNITUDE. §15.75.4 r1 answers for
      *> every accepted seed: "The returned value is greater than or equal to zero and less than one";
      *> r2 fixes reproducibility: "For a given seed value on a given implementation, the sequence of
      *> pseudo-random numbers will always be the same". §15.75.4 r3 is a FLOOR, not a domain — "The
      *> implementor shall specify the subset of the domain of argument-1 values that will yield
      *> distinct sequences of pseudo-random numbers. This subset shall include the values from 0
      *> through at least 32767" — so a seed outside that subset is still a LEGAL argument whose
      *> sequence r1/r2 define, and §15.3's closing paragraph sets EC-ARGUMENT-FUNCTION only when an
      *> argument "results in an incorrect value for that argument or for the returned value according
      *> to the rules specified in the function definition". There is no such value here, so a
      *> conforming program may pass any non-negative integer and must get a value in [0, 1), never
      *> an abort. The defect: the seed rode the PARTIAL-function narrowing, so a 19-digit seed
      *> terminated the run unit under checking and substituted the ARGUMENT 0 with checking off.
      *>
      *> ⚠ NO PSEUDO-RANDOM VALUE IS PRINTED, and none may be: r3 leaves the sequences themselves
      *> implementor-defined. Every leg asserts a RULE — the r1 range, the r2 repeat, the r3 floor's
      *> distinctness — so each expected line is derived from the standard, never observed.
      *>
      *> ⚠ WITNESSED AT 2002 THOUGH THE RULE IS 85's. RANDOM is a COBOL-85 function and the seed is
      *> total at every edition, but the WITNESS is not writable below 2002: the 19-to-31-digit
      *> ITEM is §13.18.40.3 r14's ("the number of digit positions described by
      *> character-string-1 shall range from 1 through 31") and the 19-to-31-digit LITERAL is
      *> §8.3.3.3.2's ("fixed-point numeric literals of 1 through 31 digits in length");
      *> both are 2002+, and >>TURN is the 2002 exception model. Below 2002 every
      *> writable seed fits the 64-bit window, so the behaviour does not differ by edition — only the
      *> ability to state it does.
      *>
      *> ARMED, NOT ASSUMED. A checking-ON leg proves nothing if the directive is inert, so A0 raises
      *> the condition from the ONE rule that does constrain this argument — §15.75.3 r2's sign — and
      *> the declarative reports it. Every leg after it runs under the same armed directive.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB636RND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG   PIC S9(4) VALUE -5.
       01 BIG19 PIC 9(19) VALUE 9999999999999999999.
       01 B31   PIC 9(31) VALUE
           1234567890123456789012345678901.
       01 P18   PIC 9(18) VALUE 184467440737115466.
       01 GUARD PIC 9 VALUE 9.
       01 A1    PIC 9V9(15).
       01 A2    PIC 9V9(15).
       01 B1    PIC 9V9(15).
       01 C1    PIC 9V9(15).
       01 Z0    PIC 9V9(15).
       01 Z1    PIC 9V9(15).
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
      *> ARMED — a NEGATIVE seed is the one incorrect value §15.75.3 r2 defines, so §15.3 sets the
      *> condition; the aborted COMPUTE stores nothing and GUARD still shows its 9 preset. If the
      *> directive were inert the declarative would not report and A0 would not be 9.
           MOVE 9 TO GUARD
           COMPUTE GUARD = FUNCTION RANDOM(NEG)
           DISPLAY "A0=" GUARD
      *> T1 — a 19-digit seed, above 9 223 372 036 854 775 807: §15.75.4 r1 owes a value in [0, 1).
           COMPUTE A1 = FUNCTION RANDOM(BIG19)
           IF A1 >= 0 AND A1 < 1
               DISPLAY "T1=OK"
           ELSE
               DISPLAY "T1=BAD"
           END-IF
      *> T2 — §15.75.4 r2: the SAME seed yields the same sequence, so the same first value.
           COMPUTE A2 = FUNCTION RANDOM(BIG19)
           IF A1 = A2
               DISPLAY "T2=OK"
           ELSE
               DISPLAY "T2=BAD"
           END-IF
      *> T3 — a 31-digit seed, the widest numeric item a PICTURE clause may describe
      *> (§13.18.40.3 r14), is equally unconstrained by r2.
           COMPUTE B1 = FUNCTION RANDOM(B31)
           IF B1 >= 0 AND B1 < 1
               DISPLAY "T3=OK"
           ELSE
               DISPLAY "T3=BAD"
           END-IF
      *> T4 — the §15.3 type-6 arithmetic-expression carrier: 184 467 440 737 115 466 * 100 + 62 is
      *> 18 446 744 073 711 546 662 = 2**64 + 1 995 046, past the 64-bit window by construction.
           COMPUTE C1 = FUNCTION RANDOM(P18 * 100 + 62)
           IF C1 >= 0 AND C1 < 1
               DISPLAY "T4=OK"
           ELSE
               DISPLAY "T4=BAD"
           END-IF
      *> T5 — the r3 FLOOR itself: 0 and 32767 are both inside the subset the implementor must make
      *> distinct, so their sequences differ and their first values differ with them.
           COMPUTE Z0 = FUNCTION RANDOM(0)
           COMPUTE Z1 = FUNCTION RANDOM(32767)
           IF Z0 NOT = Z1
               DISPLAY "T5=OK"
           ELSE
               DISPLAY "T5=BAD"
           END-IF
      *> T6 — and the floor's own values stay inside the r1 range.
           IF Z0 >= 0 AND Z0 < 1 AND Z1 >= 0 AND Z1 < 1
               DISPLAY "T6=OK"
           ELSE
               DISPLAY "T6=BAD"
           END-IF
           STOP RUN.
       END PROGRAM PB636RND.
