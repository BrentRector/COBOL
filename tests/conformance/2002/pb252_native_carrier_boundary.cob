      *> kb/Work PB252 — the exact Int128 carrier must never WRAP. §14.7.5 rule 5 makes an arithmetic
      *> operation that takes the intermediate outside the implementor's checked range the SIZE ERROR
      *> condition; the accumulators for SUM (§15.88.4) and RANGE (§15.76.4) were plain unchecked
      *> Int128 adds, so a conforming COMPUTE stored a SIGN-FLIPPED value through an ON SIZE ERROR
      *> phrase that was NOT taken.
      *>
      *> ⛔ NOTHING HERE PINS AN IMPLEMENTOR DETERMINATION. §15.4.1 gives native arithmetic only "an
      *> implementor-defined approximation", and §14.7.5 rule 5 leaves the intermediate's range to the
      *> implementor — so every assertion below is either forced by a rule with no latitude (case 1),
      *> or a DISJUNCTION over the only conforming outcomes (case 2), or a consequence of the returned
      *> value rules themselves (case 3).
      *>
      *> --check validated:
      *>   cite.py --check 14.7.5 "if native arithmetic is in effect and the implementor defines that
      *>     the range of values allowed for the intermediate data item is to be checked, when an
      *>     arithmetic operation on the intermediate data item would cause the new value to be
      *>     outside of the allowed range" -> OK §14.7.5 5)
      *>   cite.py --check 14.7.5 "if, after radix point alignment and any applicable rounding
      *>     specifications, the result of an arithmetic statement is further from zero than permitted
      *>     for the associated resultant data item" -> OK §14.7.5 3)
      *>   cite.py --check 14.7.5 "the values of all of the resultant data items remain unchanged from
      *>     the values they had at the start of the execution of the arithmetic statement" -> OK
      *>     §14.7.5 1) (under the SIZE ERROR phrase)
      *>   cite.py --check 15.88.3 "Argument-1 shall be of class numeric" -> OK §15.88.3 1)
      *>   cite.py --check 15.76.4 "(FUNCTION MAX (argument-list) - FUNCTION MIN (argument-list))"
      *>     -> OK §15.76.4 1)   [en dash in the source]
      *>   cite.py --check 15.4.1 "the value returned is an implementor-defined approximation of the
      *>     value of that expression" -> OK §15.4.1
      *>   cite.py --check 15.59.4 "the returned value is the content of the argument-1 having the
      *>     greatest value" -> OK §15.59.4 1)
      *>   cite.py --check 15.63.4 "the returned value is the content of the argument-1 having the
      *>     least value" -> OK §15.63.4 1)   -- together these two make MAX >= MIN, hence case (3).
      *>
      *> ⚠ EDITION. The witness needs 31-digit items: COBOL-85 caps fixed-point at 18 digits
      *> (COBOLNET0802), and eighteen-digit arguments cannot reach a 38-digit carrier in four terms.
      *> COBOL-2002 is therefore the EARLIEST edition at which this defect is expressible, which is
      *> why the file lives here rather than in 85/.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB252NAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-P    PIC S9(31) VALUE 9999999999999999999999999999999.
       01 W-Q7   PIC SV9(7) VALUE 0.
       01 W-HI   PIC S9(31) VALUE 1700000000000000000000000000000.
       01 W-LO   PIC S9(31) VALUE -1700000000000000000000000000000.
       01 W-Q8   PIC SV9(8) VALUE 0.
       01 W-R    PIC S9(31) VALUE 0.
       01 W-SE   PIC 9      VALUE 0.
       01 W-S    PIC S9(4)  VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> (1) NO LATITUDE. §15.88.4 r1c's expression here is 3 x 10**31 - 3, which is further from
      *>     zero than a 31-digit receiver permits, so §14.7.5 rule 3 makes the size error condition
      *>     exist whatever the intermediate carrier is, and rule 1 leaves W-R at its prior value.
      *>     The defect answered -4028236692093846346337460743179 with the phrase NOT taken.
           MOVE 0 TO W-SE
           MOVE 0 TO W-R
           COMPUTE W-R = FUNCTION SUM (W-P W-P W-P W-Q7)
               ON SIZE ERROR MOVE 1 TO W-SE
           END-COMPUTE
           IF W-SE = 1 AND W-R = 0
               DISPLAY "SUM-R3-SIZE-ERROR=YES"
           ELSE
               DISPLAY "SUM-R3-SIZE-ERROR=NO"
           END-IF
      *> (2) THE VALUE (kb/Work PB621 moved this from a disjunction to the value). §15.76.4 r1's
      *>     expression here is 1.7e30 - (-1.7e30) = 3.4e30, which FITS a 31-digit receiver, so rule 3
      *>     does not fire and the receiver must hold it. §14.7.5 rule 5 would PERMIT a size error
      *>     (the intermediate's range is the implementor's), and the aligned scale-8 intermediate
      *>     3.4e38 does leave the Int128 carrier — which is exactly why the arm now moves to the
      *>     SDIDI, whose exponent is carried at run time, instead of reporting a size error for a
      *>     value the receiver holds. Case (1) above remains the control for the receiver-overflow
      *>     size error (rule 3), which no carrier width can avoid.
           MOVE 0 TO W-SE
           MOVE 0 TO W-R
           COMPUTE W-R = FUNCTION RANGE (W-HI W-LO W-Q8)
               ON SIZE ERROR MOVE 1 TO W-SE
           END-COMPUTE
           IF W-SE = 0 AND W-R = 3400000000000000000000000000000
               DISPLAY "RANGE-CONFORMS=VALUE"
           ELSE
               DISPLAY "RANGE-CONFORMS=NO"
           END-IF
      *> (3) A CONSEQUENCE OF THE RULES, in every arithmetic mode: §15.76.4 r1 is MAX - MIN, and
      *>     §15.59.4 r1 / §15.63.4 r1 select the greatest and the least of the SAME argument list,
      *>     so MAX >= MIN and RANGE can never be negative.
           IF W-R < 0
               DISPLAY "RANGE-SIGN=NEGATIVE"
           ELSE
               DISPLAY "RANGE-SIGN=NON-NEGATIVE"
           END-IF
      *> (4) THE CONTROL — the same two functions well inside the carrier still answer exactly, so a
      *>     guard that simply raised everywhere would fail here. §15.88.4 r1c: 1+2+3+4 = 10.
      *>     §15.76.4 r1: MAX(1 2 3 4) - MIN(1 2 3 4) = 4 - 1 = 3.
           COMPUTE W-S = FUNCTION SUM (1 2 3 4)
           IF W-S = 10
               DISPLAY "SUM-SMALL=10"
           ELSE
               DISPLAY "SUM-SMALL=BAD"
           END-IF
           COMPUTE W-S = FUNCTION RANGE (1 2 3 4)
           IF W-S = 3
               DISPLAY "RANGE-SMALL=3"
           ELSE
               DISPLAY "RANGE-SMALL=BAD"
           END-IF
           STOP RUN.
