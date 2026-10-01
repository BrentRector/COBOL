      *> THE EXACT CARRIER'S ESCAPE BOUNDARY IS A SIZE ERROR, NEVER A WRAP.
      *> COBOLNET_NUMERIC_DESIGN.md's substrate paragraph fixes the policy: "the Int128 escape boundary is
      *> reached only when a single product ... exceeds Int128 (~38 digits) -> EC-SIZE-OVERFLOW". 15.4.1 asks at
      *> worst for "an implementor-defined approximation of the value of that expression", and a value produced by
      *> a silent modulo-2^128 wrap approximates nothing.
      *>
      *> ⛔ THIS GOLDEN EXISTS BECAUSE MEDIAN AND MIDRANGE SPENT A DIGIT THEIR SIBLINGS DO NOT (fix-queue PB32).
      *> Both return at scale common+1 so that their halving is exact - odd MEDIAN multiplies the middle by 10,
      *> even MEDIAN and MIDRANGE multiply the sum by 5 - and that scale bump costs a decimal digit of Int128
      *> headroom which MAX, MIN, SUM and RANGE never spend. So these two wrapped at ONE FIFTH the magnitude their
      *> siblings survive, silently, with an ON SIZE ERROR phrase present and NOT taken. MEASURED before the fix:
      *>     MAX      -> 99999999999999999999999999999.98   (exact)
      *>     MIN      ->                             0.00   (exact)
      *>     MIDRANGE -> 15971763307906153653662539256.81   (WRONG, and no size error)
      *>     hand EAE -> 49999999999999999999999999999.99   (correct, same compiler, same run)
      *> The compiler contradicting its OWN written 15.62.4 equivalent arithmetic expression, on legal source, is
      *> what makes this MIDRANGE's own defect rather than shared alignment residue: MAX and MIN are both right.
      *>
      *> ⚠ THE GUARD MUST NOT OVER-FIRE. The ordinary-magnitude cases below are the other half of the evidence -
      *> a boundary check that also broke MEDIAN(1 2 3 4) would trade a silent wrong answer for a loud one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB32HALVING.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P  PIC S9(29)V99 VALUE 99999999999999999999999999999.98.
       01 Q  PIC SV9(9)    VALUE 0.000000002.
       01 R  PIC S9(29)V99.
       01 S  PIC S9(9)V99.
       PROCEDURE DIVISION.
      *> MAX and MIN are exact at this magnitude - they return at the common scale, with no halving bump.
           COMPUTE R = FUNCTION MAX(P Q)
           DISPLAY "MAX=" R
           COMPUTE R = FUNCTION MIN(P Q)
           DISPLAY "MIN=" R
      *> MIDRANGE needs one decimal digit more than MAX/MIN ON THE Int128 CARRIER and no longer had it, so it was a
      *> SIZE ERROR here (14.7.5 - the intermediate's range is the implementor's, and the receiver was unchanged).
      *> kb/Work PB621 moved that boundary: the aligned arguments' digit bounds (31 + 9 > 38) route the call to the
      *> SDIDI body, which holds the value the receiver holds - (MAX + MIN) / 2 = 49999999999999999999999999999.99
      *> truncated, exactly the hand-written 15.62.4 r1 expression below.  No size error is raised.
           MOVE 0 TO R
           COMPUTE R = FUNCTION MIDRANGE(P Q)
               ON SIZE ERROR DISPLAY "MIDRANGE-SIZE-ERROR"
           END-COMPUTE
           DISPLAY "MIDRANGE-RECEIVER=" R
      *> The same value the standard's own equivalent arithmetic expression yields, written by hand.
           COMPUTE R = (FUNCTION MAX(P Q) + FUNCTION MIN(P Q)) / 2
           DISPLAY "HAND-EAE=" R
      *> ⚠ ORDINARY MAGNITUDES MUST BE UNAFFECTED - the even branch, the odd branch, and MIDRANGE.
           COMPUTE S = FUNCTION MEDIAN(1 2 3 4)
           DISPLAY "MEDIAN-EVEN=" S
           COMPUTE S = FUNCTION MEDIAN(1 2 3)
           DISPLAY "MEDIAN-ODD=" S
           COMPUTE S = FUNCTION MIDRANGE(1 2 3 10)
           DISPLAY "MIDRANGE-SMALL=" S
           STOP RUN.
