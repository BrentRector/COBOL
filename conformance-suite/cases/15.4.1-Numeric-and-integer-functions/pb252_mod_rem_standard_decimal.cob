*>--- conformance-case
*> id: pb252_mod_rem_standard_decimal
*> rules: [RV-15.4.1-L2.1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB252 — under ARITHMETIC IS STANDARD-DECIMAL §15.4.1 rule 1 leaves NO latitude: "the
      *> returned value shall equal the value of the equivalent arithmetic expression", and
      *> §8.8.1.5.2 rule 1 converts each argument to the SDIDI INDIVIDUALLY, so no common scale is
      *> ever formed. The native arms align first, on the Int128 carrier, and alignment multiplies —
      *> a 31-digit integer beside a scale-18 operand needs 49 digits. MOD and REM were still routed
      *> there, so this program terminated the run unit with EC-SIZE-OVERFLOW where the rule fixes an
      *> exact value.
      *>
      *> --check validated:
      *>   cite.py --check 15.4.1 "the returned value shall equal the value of the equivalent
      *>     arithmetic expression" -> OK §15.4.1 1)
      *>   cite.py --check 15.77.3 "Argument-1 and argument-2 shall be of class numeric" -> OK
      *>     §15.77.3 1)  — so REM's scale-18 argument-2 below is CONFORMING source.
      *>   cite.py --check 15.64.3 "Argument-1 and argument-2 shall be integers" -> OK §15.64.3 1)
      *>     — so MOD's arguments are integers, the common scale is always 0, and MOD is here to pin
      *>     that its SDIDI routing did not change the value, not to carry a wide witness.
      *>   cite.py --check 15.49.4 "The equivalent arithmetic expression is" -> OK §15.49.4 1)
      *>     (INTEGER-PART, which §15.77.4 r1's expression invokes)
      *>   cite.py --check 8.8.1.5.2 "Any operand of an arithmetic expression that is not already in
      *>     SDIDI form is converted into this form" -> OK §8.8.1.5.2 1)   -- per OPERAND, so the
      *>     standard mode never forms the common scale the native arm aligns to.
      *>
      *> HAND-DERIVED, from §15.77.4 r1's expression a - (b * FUNCTION INTEGER-PART (a / b)):
      *>   a = 9999999999999999999999999999998, b = 1.5
      *>   a / b            = 6666666666666666666666666666665.333...
      *>   INTEGER-PART     = 6666666666666666666666666666665          (§15.49.4, toward zero)
      *>   b * that         = 9999999999999999999999999999997.5
      *>   a - that         = 0.5                                       <- the required value
      *> Every intermediate above is at most 32 significant digits, so a 34-digit SDIDI holds the
      *> chain exactly (§8.8.1.5.2) and the rule's value is reachable; the 49-digit ALIGNED form on
      *> the Int128 carrier is not, which is the whole point.
      *>
      *> The four MOD rows are the §15.64.4 NOTE's own table, printed in the standard.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB252SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A  PIC 9(31)   VALUE 9999999999999999999999999999998.
       01 W-B  PIC 9V9(18) VALUE 1.5.
       01 W-R  PIC 9V9(18) VALUE 0.
       01 W-M  PIC S9(4)   VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-R = FUNCTION REM (W-A W-B)
           IF W-R = 0.5
               DISPLAY "REM-WIDE=0.5"
           ELSE
               DISPLAY "REM-WIDE=BAD"
           END-IF
           COMPUTE W-M = FUNCTION MOD (11 5)
           IF W-M = 1
               DISPLAY "MOD-11-5=1"
           ELSE
               DISPLAY "MOD-11-5=BAD"
           END-IF
           COMPUTE W-M = FUNCTION MOD (-11 5)
           IF W-M = 4
               DISPLAY "MOD-N11-5=4"
           ELSE
               DISPLAY "MOD-N11-5=BAD"
           END-IF
           COMPUTE W-M = FUNCTION MOD (11 -5)
           IF W-M = -4
               DISPLAY "MOD-11-N5=-4"
           ELSE
               DISPLAY "MOD-11-N5=BAD"
           END-IF
           COMPUTE W-M = FUNCTION MOD (-11 -5)
           IF W-M = -1
               DISPLAY "MOD-N11-N5=-1"
           ELSE
               DISPLAY "MOD-N11-N5=BAD"
           END-IF
      *> REM's own sign rule (§15.77.4 r1 truncates toward zero, unlike MOD's floor): the same two
      *> operand pairs give -1 and 1 where MOD gives 4 and -4.
           COMPUTE W-M = FUNCTION REM (-11 5)
           IF W-M = -1
               DISPLAY "REM-N11-5=-1"
           ELSE
               DISPLAY "REM-N11-5=BAD"
           END-IF
           COMPUTE W-M = FUNCTION REM (11 -5)
           IF W-M = 1
               DISPLAY "REM-11-N5=1"
           ELSE
               DISPLAY "REM-11-N5=BAD"
           END-IF
           STOP RUN.
