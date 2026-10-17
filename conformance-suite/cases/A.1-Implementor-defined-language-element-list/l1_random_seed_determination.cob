*>--- conformance-case
*> id: l1_random_seed_determination
*> rules: [DOC-A.1-144]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.75.3 r4 — the RANDOM function's SEED VALUE when the FIRST reference in the run
      *> unit does not specify argument-1 (Annex A.1 item 144; docs/CONFORMANCE.md DOC-A.1-144).
      *>
      *> THE RULE. §15.75.3 r4: "If the first reference to this function in the run unit does
      *> not specify argument-1, the seed value is defined by the implementor." Annex A.1 item
      *> 144 is the one implementor obligation whose VALUE need not be documented, so what a
      *> conforming program can observe about it is exactly this: the branch EXISTS — an
      *> unseeded first reference is legal and yields a value — and the value obeys the
      *> function's own returned-value rules. COBOL.NET's determination is per-process OS
      *> entropy (no fixed seed), which is by construction not reproducible across runs and so
      *> is not, and must not be, spelled as a literal in any golden.
      *>
      *> ⛔ THIS IS THE BRANCH NO OTHER GOLDEN REACHES. conformance:cases/15.75.3-Argument-rules/pb1_intrinsic_formats
      *> exercises r5 and returned-value r2, but its FIRST reference is FUNCTION RANDOM(7), so
      *> r4's precondition ("the first reference ... does not specify argument-1") is false
      *> there. Here the first reference in the run unit is the bare FUNCTION RANDOM below —
      *> §15.75.2's general format `FUNCTION RANDOM [ ( [ argument-1 ] ) ]` makes it a complete
      *> reference — and every later leg is written after it on purpose.
      *>
      *> FIRST  - r4's branch, bounded by §15.75.4 r1: "The returned value is greater than or
      *>          equal to zero and less than one." The receiving item is SIGNED with two
      *>          integer positions so a negative or out-of-range result survives the store and
      *>          is caught by the test rather than being masked by truncation into range.
      *> NEXT   - §15.75.3 r5: "In each case, subsequent references without specifying
      *>          argument-1 return the next number in the current sequence" — the sequence r4
      *>          started is a real sequence and keeps returning r1-legal values.
      *> RESEED - §15.75.3 r3: "If a subsequent reference specifies argument-1, a new sequence
      *>          of pseudo-random numbers is started", and §15.75.4 r2: "For a given seed
      *>          value on a given implementation, the sequence of pseudo-random numbers will
      *>          always be the same." Two consecutive references that each specify seed 7
      *>          therefore each start seed 7's sequence and each return its FIRST number, so
      *>          the two values are EQUAL. This is the leg that proves the unseeded start of
      *>          FIRST did not leave the generator in a state that a re-seed cannot displace.
      *> SEED0  - the lowest seed §15.75.3 r2 admits ("it shall be zero or a positive integer")
      *>          and the low end of the domain subset §15.75.4 r3 requires ("shall include the
      *>          values from 0 through at least 32767"), still bounded by r1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RNDSD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V1 PIC S9(2)V9(9).
       01 V2 PIC S9(2)V9(9).
       01 OK PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE V1 = FUNCTION RANDOM
           MOVE "NO" TO OK
           IF V1 >= 0 AND V1 < 1
               MOVE "YES" TO OK
           END-IF
           DISPLAY "FIRST=" OK
           COMPUTE V2 = FUNCTION RANDOM
           MOVE "NO" TO OK
           IF V2 >= 0 AND V2 < 1
               MOVE "YES" TO OK
           END-IF
           DISPLAY "NEXT=" OK
           COMPUTE V1 = FUNCTION RANDOM(7)
           COMPUTE V2 = FUNCTION RANDOM(7)
           MOVE "NO" TO OK
           IF V1 = V2
               MOVE "YES" TO OK
           END-IF
           DISPLAY "RESEED=" OK
           COMPUTE V1 = FUNCTION RANDOM(0)
           MOVE "NO" TO OK
           IF V1 >= 0 AND V1 < 1
               MOVE "YES" TO OK
           END-IF
           DISPLAY "SEED0=" OK
           STOP RUN.
