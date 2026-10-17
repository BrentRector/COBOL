*>--- conformance-case
*> id: pb249_trim_argument2_repeats
*> rules: [AR-15.96.3-2, AR-15.96.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB249 - ISO 15.96.3 r2/r3 over the REPEATING argument-2. 15.96.2 prints `[ argument-2 ] ...`,
      *> so r2 - "argument-2 shall be a single character that is either class alphabetic or class alphanumeric.
      *> When argument-1 is class national, argument-2 shall be a single character of class national" - governs
      *> EVERY argument-2, not only the first; the schema declared two positions with no variadic tail, so from
      *> the SECOND argument-2 onward nothing screened the width, the class or the cross-argument agreement.
      *> This fixture is r2's NEGATIVE SPACE (the two rejections live in negative/pb249-trim-arg2-*): it proves
      *> the new tail screen does not over-reject the legal repeated forms, and it pins r3 and the run-time half.
      *> Every value below is derived from 15.96.4 r5 - "If multiple argument-2s are specified, each argument-2 is
      *> processed completely in the order that they are specified prior to processing the next argument-2" -
      *> i.e. TRIM(a b c) = TRIM(TRIM(a b) c), never a set union:
      *>   TWO: TRIM("aabbccaa" "a" "b") = TRIM("bbcc" "b")          = "cc"
      *>   THR: TRIM("xyzABCzyx" "x" "y" "z") = TRIM(TRIM("yzABCzy" "y") "z") = TRIM("zABCz" "z") = "ABC"
      *>   ORD: TRIM("bcab" "c" "b")  - the ORDER-carrying pair (kb/Work PB117): the inner fold strips nothing
      *>        ('c' guards neither edge), so the answer is TRIM("bcab" "b") = "ca"; a set union would give "a".
      *>   NAT: argument-1 class national with TWO national argument-2s - r2's second sentence at the TAIL.
      *>        TRIM(N"xyABCDyx" N"x" N"y") = TRIM(N"yABCDy" N"y") = N"ABCD".
      *>   DEF: no argument-2 at all - r3 a), "argument-2 is as though an alphanumeric space had been specified".
      *>   ECV: a REFERENCE-MODIFIED argument-2 whose length is a run-time value. The bind-time ExactWidth(1)
      *>        predicate cannot decide it (KnownWidth is null) and fails OPEN, so r2 is a VALUE constraint on
      *>        this path and 15.3 rule 14 governs - "If the evaluation of an argument results in an incorrect
      *>        value for that argument ... the EC-ARGUMENT-FUNCTION exception condition is set to exist". With
      *>        checking off the substituted result is docs/CONFORMANCE.md DOC-A.1-90's zero-length value, which
      *>        a receiving item pads to spaces. Before PB249 this path silently applied r3's SPACE default -
      *>        r3's answer where r3's antecedent is false.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB249TRIM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S   PIC X(8)  VALUE "aabbccaa".
       01 W-T   PIC X(9)  VALUE "xyzABCzyx".
       01 W-O   PIC X(4)  VALUE "bcab".
       01 W-P   PIC X(10) VALUE "  HELLO   ".
       01 W-D   PIC X(4)  VALUE "ab  ".
       01 W-L   PIC 9(2)  VALUE 2.
       01 W-N   PIC N(8)  VALUE N"xyABCDyx".
       01 W-R   PIC X(12).
       01 W-NR  PIC N(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TRIM(W-S "a" "b") TO W-R
           DISPLAY "TWO=[" W-R "]"
           MOVE FUNCTION TRIM(W-T "x" "y" "z") TO W-R
           DISPLAY "THR=[" W-R "]"
           MOVE FUNCTION TRIM(W-O "c" "b") TO W-R
           DISPLAY "ORD=[" W-R "]"
           MOVE FUNCTION TRIM(W-N N"x" N"y") TO W-NR
           DISPLAY "NAT=[" W-NR "]"
           MOVE FUNCTION TRIM(W-P) TO W-R
           DISPLAY "DEF=[" W-R "]"
           MOVE FUNCTION TRIM(W-S W-D(1:W-L)) TO W-R
           DISPLAY "ECV=[" W-R "]"
           STOP RUN.
