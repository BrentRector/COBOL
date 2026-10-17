      *>--- conformance-case
      *> id: l1_random_unseeded_first_reference
      *> rules: [AR-15.75.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.75.3 r4 — RANDOM's UNSEEDED FIRST reference in the run
      *> unit: "If the first reference to this function in the run unit
      *> does not specify argument-1, the seed value is defined by the
      *> implementor." The implementor supplies a seed, so the reference
      *> is well defined and it is what starts the CURRENT sequence.
      *>   cite.py --check 15.75.3 "If the first reference to this
      *>   function in the run unit does not specify argument-1, the
      *>   seed value is defined by the implementor."
      *>     -> OK  §15.75.3 4)  (Argument rules)
      *>
      *> ⛔ WHY A NEW GOLDEN. 2023/pb1_intrinsic_formats seeds its FIRST
      *> reference (COMPUTE A = FUNCTION RANDOM(7)), so r4's antecedent
      *> — "the FIRST reference ... does not specify argument-1" — is
      *> never entered there. 2023/pb9_reserved_intrinsic_names DOES
      *> enter it (COMPUTE R = RANDOM is its first reference), but its
      *> is PIC 9V9(4): an UNSIGNED display receiver stores the ABSOLUTE
      *> value, so a negative return would read back in range and the
      *> RND-K-IN-RANGE leg would still pass. Every receiver here is
      *> COMP-2, which carries the sign, so the range leg can fail.
      *>
      *> EVERY LINE DERIVED, r4 first:
      *>  R4-FIRST=IN-RANGE  r4 makes the unseeded first reference well
      *>    defined — a seed exists, therefore a sequence exists — and
      *>    §15.75.4 r1 fixes its codomain: "The returned value is
      *>    greater than or equal to zero and less than one."
      *>      cite.py --check 15.75.4 "The returned value is greater
      *>      than or equal to zero and less than one."
      *>        -> OK  §15.75.4 1)  (Returned value rules)
      *>  R4-NEXT=IN-RANGE   §15.75.3 r5: "In each case, subsequent
      *>    references without specifying argument-1 return the next
      *>    number in the current sequence" — the sequence r4's own seed
      *>    started; §15.75.4 r1 bounds it again.
      *>  R4-SEQ=ADVANCES    r5's "the NEXT number" of a §15.75.1
      *>    "pseudo-random number from a rectangular distribution": two
      *>    successive draws of a continuous rectangular distribution
      *>    coincide only on a measure-zero event. STRUCTURAL, the same
      *>    convention 2023/pb1_intrinsic_formats' SEQ-ADVANCES uses;
      *>    no implementation number is written down anywhere below.
      *>  R3-RESEED=REPRODUCES  §15.75.3 r3: "If a subsequent reference
      *>    specifies argument-1, a new sequence of pseudo-random
      *>    numbers is started" — so each FUNCTION RANDOM(7) restarts
      *>    seed 7's sequence and returns its FIRST number — and
      *>    §15.75.4 r2: "For a given seed value on a given
      *>    implementation, the sequence of pseudo-random numbers will
      *>    always be the same" — so the two firsts are equal, whatever
      *>    the numbers are. This is r4's OTHER half: the implicit seed
      *>    must leave ONE current sequence that an explicit seed
      *>    REPLACES, not a second generator an explicit seed misses.
      *>
      *> The seed VALUE is deliberately unobservable and no golden may
      *> pin a number here: docs/CONFORMANCE.md DOC-A.1-144 records the
      *> r4 determination as per-process OS entropy, so a sequence is
      *> reproducible only from an explicit FUNCTION RANDOM(seed).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RND01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V1 USAGE COMP-2.
       01 V2 USAGE COMP-2.
       01 W1 USAGE COMP-2.
       01 W2 USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
      *> r4 — THE FIRST reference to RANDOM in this run unit, and it
      *> specifies no argument-1.
           COMPUTE V1 = FUNCTION RANDOM
           IF V1 < 0 OR V1 NOT < 1
               DISPLAY "R4-FIRST=OUT-OF-RANGE"
           ELSE
               DISPLAY "R4-FIRST=IN-RANGE"
           END-IF
      *> r5 — the next number of the sequence r4's seed started.
           COMPUTE V2 = FUNCTION RANDOM
           IF V2 < 0 OR V2 NOT < 1
               DISPLAY "R4-NEXT=OUT-OF-RANGE"
           ELSE
               DISPLAY "R4-NEXT=IN-RANGE"
           END-IF
           IF V1 = V2
               DISPLAY "R4-SEQ=STUCK"
           ELSE
               DISPLAY "R4-SEQ=ADVANCES"
           END-IF
      *> r3 + §15.75.4 r2 — an explicit seed REPLACES the sequence r4
      *> started, and seed 7 reproduces.
           COMPUTE W1 = FUNCTION RANDOM(7)
           COMPUTE W2 = FUNCTION RANDOM(7)
           IF W1 = W2
               DISPLAY "R3-RESEED=REPRODUCES"
           ELSE
               DISPLAY "R3-RESEED=VARIES"
           END-IF
           STOP RUN.
       END PROGRAM L1RND01.
