*>--- conformance-case
*> id: argument_rule_equivalences_batch1
*> rules: [AR-15.19.3-2, AR-15.19.3-6, AR-15.25.3-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> Two ARGUMENT RULES adjudicated CONFORMS with no spec-derived test, so each stayed a GAP:
      *> AR-15.19.3-2 (CONVERT) and AR-15.25.3-3 (DAY-TO-YYYYDDD). Both rules state an EQUIVALENCE, which is
      *> directly testable: write the construct both ways and assert the results are identical. That is a
      *> stronger assertion than pinning one literal answer, because it fails if EITHER spelling drifts - and
      *> it needs no knowledge of what the shared answer is. ⛔ Each line also PRINTS the shared value, so
      *> the assertion cannot pass VACUOUSLY - an equality test that compared two empty or two identically
      *> broken results would still print "OK", and the printed value is what makes that visible.
      *>
      *>   15.19.3 r2 - "ALPHANUMERIC and ANUM are equivalent. NATIONAL and NAT are equivalent." Per 15.19.2
      *>                those words appear in BOTH the source-format and the destination-format, so the
      *>                equivalence is asserted in both positions. (The destination-format is TWO words - one
      *>                of {ALPHANUMERIC|ANUM|NAT|NATIONAL} followed by the required word HEX - or the single
      *>                word BYTE; a bare HEX destination is not legal and is not what this tests.)
      *>   15.25.3 r3 - "If argument-2 is omitted, the function shall be evaluated as though 50 were specified
      *>                for argument-2." Argument-2 is the windowing cut, so the omitted and explicit-50 forms
      *>                are compared on BOTH sides of 50 and at the boundary itself. A single sample above or
      *>                below the cut would pass while the windowing was wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ARGRULEEQV1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC    PIC X(2) VALUE "AB".
       01 NSRC   PIC NN   VALUE N"AB".
       01 L1     PIC X(8).
       01 L2     PIC X(8).
       01 D-OMIT PIC 9(7).
       01 D-50   PIC 9(7).
       01 BAD    PIC 9 VALUE 0.
       PROCEDURE DIVISION.
      *> AR-15.19.3-2, SOURCE position: ALPHANUMERIC = ANUM.
           MOVE FUNCTION CONVERT (SRC ALPHANUMERIC ANUM HEX) TO L1
           MOVE FUNCTION CONVERT (SRC ANUM         ANUM HEX) TO L2
           IF L1 = L2 DISPLAY "SRC-ANUM-EQV=OK " L1
              ELSE DISPLAY "SRC-ANUM-EQV=BAD " L1 " " L2 END-IF

      *> AR-15.19.3-2, DESTINATION position: ALPHANUMERIC = ANUM.
           MOVE FUNCTION CONVERT (SRC ANUM ALPHANUMERIC HEX) TO L1
           MOVE FUNCTION CONVERT (SRC ANUM ANUM         HEX) TO L2
           IF L1 = L2 DISPLAY "DST-ANUM-EQV=OK " L1
              ELSE DISPLAY "DST-ANUM-EQV=BAD " L1 " " L2 END-IF

      *> AR-15.19.3-2, the NATIONAL/NAT half of the same rule, in the SOURCE position. The source item is
      *> NATIONAL: 15.19.3 r6 reads a NAT-source argument-1 as a string of national characters, so a PIC X
      *> item there is non-conforming (the PB59 family-5a screen rejects it) - the original PIC X SRC was a
      *> transcription-era card error of this golden, corrected 2026-08-09; the printed value is unchanged
      *> because UTF-16BE "AB" hexes to 00410042 from either declaration.
           MOVE FUNCTION CONVERT (NSRC NATIONAL ANUM HEX) TO L1
           MOVE FUNCTION CONVERT (NSRC NAT      ANUM HEX) TO L2
           IF L1 = L2 DISPLAY "SRC-NAT-EQV=OK " L1
              ELSE DISPLAY "SRC-NAT-EQV=BAD " L1 " " L2 END-IF

      *> AR-15.25.3-3 - the omitted argument-2 must behave EXACTLY as an explicit 50. 15.25.3 r1 bounds
      *> argument-1 below 100000, and argument-2 is the windowing cut, so these probe below it, at it, and
      *> above it: years 12, 49, 50, 51 and 75.
           PERFORM CHECK-12345 THRU CHECK-EXIT
      *> ⛔ The VALUE is not printed: 15.25.3 r5 defaults argument-3 to the CURRENT YEAR, so the YYYY half of
      *> the result is a clock reading and would drift. The EQUALITY is still clock-independent (both sides read
      *> the same clock in the same run), and the DDD half is exactly what was supplied, so the day component is
      *> printed as the non-vacuity witness - the PB7 convention for a value that is not reproducible.
           COMPUTE D-50 = FUNCTION MOD (D-OMIT, 1000)
           IF BAD = 0 DISPLAY "DAY50-EQV=OK DDD=" D-50
              ELSE DISPLAY "DAY50-EQV=BAD" END-IF
           STOP RUN.

       CHECK-12345.
           MOVE 12001 TO D-OMIT
           PERFORM COMPARE-ONE
           MOVE 49001 TO D-OMIT
           PERFORM COMPARE-ONE
           MOVE 50001 TO D-OMIT
           PERFORM COMPARE-ONE
           MOVE 51001 TO D-OMIT
           PERFORM COMPARE-ONE
           MOVE 75001 TO D-OMIT
           PERFORM COMPARE-ONE.
       CHECK-EXIT.
           EXIT.

       COMPARE-ONE.
           COMPUTE D-50 = FUNCTION DAY-TO-YYYYDDD (D-OMIT, 50)
           COMPUTE D-OMIT = FUNCTION DAY-TO-YYYYDDD (D-OMIT)
           IF D-OMIT NOT = D-50 MOVE 1 TO BAD END-IF.
