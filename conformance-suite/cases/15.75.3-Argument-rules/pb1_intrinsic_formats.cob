*>--- conformance-case
*> id: pb1_intrinsic_formats
*> rules: [AR-15.75.3-5, FMT-15.71.2, FMT-15.72.2, FMT-15.74.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> The §15 GENERAL FORMATS of ORD-MAX, ORD-MIN and PRESENT-VALUE, plus RANDOM's sequence rules.
      *> These rows stood as CONFORMS-but-untested because the only tests touching them were NIST goldens,
      *> which are REGRESSION NETS and cannot close a conformance row (CLAUDE.md rule 1). Every value below
      *> is derived from the spec:
      *>   15.71.2 FUNCTION ORD-MAX ( {argument-1} … )  variadic; 15.71.4 r3 leftmost wins on a tie
      *>   15.72.2 FUNCTION ORD-MIN ( {argument-1} … )  variadic
      *>   15.74.2 FUNCTION PRESENT-VALUE ( argument-1 {argument-2} … )
      *>   15.74.4 r1: for one occurrence, argument-2 / (1 + argument-1); for two, that plus
      *>               argument-2b / (1 + argument-1)**2.  With rate 1.0: 100/2 = 50; 100/2 + 100/4 = 75.
      *>   15.75.3 r5 + 15.75.4 r2: a reference WITHOUT argument-1 returns the NEXT number in the current
      *>               sequence, and a given seed always yields the same sequence. Both are asserted
      *>               STRUCTURALLY (differs / reproduces), never as an implementation's own numbers.
      *> R is UNSIGNED so DISPLAY renders plain digits: the golden is about the FORMATS, not about the
      *> zoned sign overpunch a signed DISPLAY item would add (pinned by its own goldens elsewhere).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1FORMATS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9(6)V99 VALUE 0.
       01 N    PIC 9(3).
       01 A    USAGE COMP-2.
       01 B    USAGE COMP-2.
       01 C    USAGE COMP-2.
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION ORD-MAX(3 9 1)
           DISPLAY N
           COMPUTE N = FUNCTION ORD-MIN(3 9 1)
           DISPLAY N
           COMPUTE N = FUNCTION ORD-MAX(7 7 2)
           DISPLAY N
           COMPUTE R = FUNCTION PRESENT-VALUE(1.0 100)
           DISPLAY R
           COMPUTE R = FUNCTION PRESENT-VALUE(1.0 100 100)
           DISPLAY R
           COMPUTE A = FUNCTION RANDOM(7)
           COMPUTE B = FUNCTION RANDOM
           COMPUTE C = FUNCTION RANDOM
           IF B NOT = C DISPLAY "SEQ-ADVANCES" ELSE DISPLAY "SEQ-STUCK" END-IF
           COMPUTE A = FUNCTION RANDOM(7)
           COMPUTE C = FUNCTION RANDOM
           IF B = C DISPLAY "SEED-REPRODUCES" ELSE DISPLAY "SEED-VARIES" END-IF
           STOP RUN.
