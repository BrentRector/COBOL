*>--- conformance-case
*> id: pb654_prohibited_gate_one_evaluation
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB654 at COBOL-2014 -- ONE INITIAL EVALUATION, AND ONE 14.7.4.3 r7 GATE OVER IT, FOR EVERY
      *> FIXED-SCALE RECEIVER CATEGORY. ROUNDED MODE IS is a COBOL-2014 introduction, so this is the earliest
      *> edition that can state the defect at all (COBOLNET0803 below it -- the negative is
      *> tests/conformance/negative/pb653-nested-float-rounded-mode-below-2014).
      *>
      *> (1) THE DOUBLE EVALUATION. ArithmeticEmitter.StoreArith wrote the right-hand side's expression TWICE
      *> -- once into the r7 representability test and once into the store that follows. 14.7.7 rule 4 a) says
      *> "The initial evaluation of the statement is done and the result of this operation is placed in an
      *> intermediate data item" and rule 4 b) says "the intermediate data item is stored in or combined with
      *> and then stored in each single resulting data item": ONE evaluation, held, tested, then stored. For a
      *> function reference whose successive references are not the same value the second spelling is a second
      *> returned value -- 15.75.3 rule 5: "In each case, subsequent references without specifying argument-1
      *> return the next number in the current sequence" -- so the test examined one element and the store
      *> landed the NEXT one, and the gated statement consumed two elements of the sequence instead of one.
      *>
      *> (2) THE GATE'S MISSING ARMS. 14.7.4.3 rule 7: "If the PROHIBITED phrase is specified, and the
      *> arithmetic value cannot be represented exactly in the resultant identifier, the EC-SIZE-TRUNCATION
      *> exception condition is set to exist, the size error condition exists" -- and the receiver is left
      *> unchanged. CobolFloat.ToScaled lands a PROHIBITED transfer TRUNCATED by design, so the test must
      *> precede the store. Only the plain-numeric arm had it; the numeric-EDITED arm silently stored the
      *> truncated value (measured: SQRT(3) into PIC ZZ9.999 stored 1.732 and raised nothing).
      *>
      *> NO RAW PSEUDO-RANDOM VALUE IS PINNED HERE, deliberately -- 15.75.4 rule 2 promises only that "For a
      *> given seed value on a given implementation, the sequence of pseudo-random numbers will always be the
      *> same", which is exactly enough to compare the THIRD element of one seeded run against the element the
      *> next reference returns in another. A1 proves the measurement is not vacuous: consecutive elements of
      *> the sequence must differ, or "which element did I get" could not distinguish anything.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB654PG14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC 9V9(9).
       01 ED  PIC ZZ9.999.
       01 C2  USAGE COMP-2.
       01 E2  USAGE COMP-2.
       01 E3  USAGE COMP-2.
       01 E4  USAGE COMP-2.
       01 S   USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
      *> A -- the control run: elements 2, 3 and 4 after seed 7, and the proof they are distinguishable.
           COMPUTE R  = FUNCTION RANDOM(7)
           COMPUTE E2 = FUNCTION RANDOM
           COMPUTE E3 = FUNCTION RANDOM
           COMPUTE E4 = FUNCTION RANDOM
           IF E2 NOT = E3 AND E3 NOT = E4
               DISPLAY "A1=DISTINCT"
           ELSE
               DISPLAY "A1=NOT-DISTINCT"
           END-IF
      *> B -- a gated statement whose value IS exactly representable (1 ** anything is 1.0): no size error,
      *> the value stores, and exactly ONE element of the sequence is consumed.
           COMPUTE R  = FUNCTION RANDOM(7)
           COMPUTE C2 = 1
           COMPUTE R ROUNDED MODE IS PROHIBITED = C2 ** FUNCTION RANDOM
               ON SIZE ERROR DISPLAY "B1=SIZE-ERROR"
           END-COMPUTE
           DISPLAY "B2=" R
           COMPUTE S = FUNCTION RANDOM
           IF S = E3
               DISPLAY "B3=ONE-EVALUATION"
           ELSE
               DISPLAY "B3=NOT-ONE-EVALUATION"
           END-IF
      *> C -- a gated statement whose value is NOT representable at the receiver's nine fraction digits: the
      *> size error condition exists, the receiver is unchanged, and STILL exactly one element is consumed.
           COMPUTE R  = FUNCTION RANDOM(7)
           MOVE 0.5 TO R
           COMPUTE R ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM
               ON SIZE ERROR DISPLAY "C1=SIZE-ERROR"
           END-COMPUTE
           DISPLAY "C2=" R
           COMPUTE S = FUNCTION RANDOM
           IF S = E3
               DISPLAY "C3=ONE-EVALUATION"
           ELSE
               DISPLAY "C3=NOT-ONE-EVALUATION"
           END-IF
      *> D -- the SAME rule 7 gate over a numeric-EDITED resultant, which is the arm that had none.
           COMPUTE C2 = FUNCTION SQRT(3)
           MOVE 0 TO ED
           COMPUTE ED ROUNDED MODE IS PROHIBITED = C2 * 1
               ON SIZE ERROR DISPLAY "D1=SIZE-ERROR"
           END-COMPUTE
           DISPLAY "D2=" ED
           COMPUTE C2 = 2
           COMPUTE ED ROUNDED MODE IS PROHIBITED = C2 * 1
               ON SIZE ERROR DISPLAY "D3=SIZE-ERROR"
           END-COMPUTE
           DISPLAY "D4=" ED
           STOP RUN.
