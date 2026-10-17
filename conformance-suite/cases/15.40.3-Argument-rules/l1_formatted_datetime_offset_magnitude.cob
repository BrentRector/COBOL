*>--- conformance-case
*> id: l1_formatted_datetime_offset_magnitude
*> rules: [AR-15.40.3-5, AR-15.40.3-6]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r5 — FORMATTED-DATETIME's argument-4 MAGNITUDE bound, both arms.
      *> "Argument-4 is an integer specifying the offset from UTC expressed in minutes. If argument-4 is
      *> specified, the magnitude of the value shall be less than or equal to 1439."
      *> (cite.py --check 15.40.3 "the magnitude of the value shall be less than or equal to 1439" -> OK,
      *> §15.40.3 rule 5.) The clause's own NOTE explains the number: "The offset value 1439 represents
      *> 23 hours 59 minutes, which is one minute less than a day."
      *>
      *> ⛔ THE RULE'S OBLIGATION IS THE EXCEPTION, NOT A VALUE. §15.3 item 14: an argument that "results in
      *> an incorrect value for that argument ... according to the rules specified in the function
      *> definition" sets EC-ARGUMENT-FUNCTION; the RESULT is implementor-defined and only when checking is
      *> not enabled. So this program turns checking ON and asserts the exception. Enforced NOWHERE before
      *> kb/Work PB11 landed CobolDate.OffsetOutOfRange: 2000 minutes rendered offset-hours 33 and 9999
      *> rendered 166, both violating §15.3.3.6.2 ("The offset-hours subfield ... shall contain a value from
      *> 00 to 23 inclusive") and the second one overflowing the width the format describes — a fabricated
      *> value with no exception condition, never mere over-acceptance.
      *>
      *> THE FORMAT IS AN OFFSET FORMAT, which is what §15.40.3 r6 requires before argument-4 may be written
      *> at all (its violation is conformance:cases/15.40.3-Argument-rules/l1-formatted-datetime-offset-local-format). §15.40.4
      *> r3 then fixes the expectation without any arithmetic: "the value in argument-3 is reflected DIRECTLY
      *> in the time portion of the returned value, and the offset in argument-4 is reflected DIRECTLY in the
      *> offset portion" — so the time never moves and only the offset field changes across the five probes.
      *>
      *> Hand-derived values. Integer date 153569 is 2021-06-16 (§15.5.2 epoch 1601-01-01 = 1, as
      *> 2014/formatted_datetime pins); 45296 seconds is 12:34:56 (§15.5.5). §15.3.3.6.1 gives the EXTENDED
      *> offset subformat as "six characters: a plus sign; two lowercase 'h' characters representing the
      *> offset-hours subfield; a colon character; and two lowercase 'm' characters representing the
      *> offset-minutes subfield", and its later paragraph fixes probe 3's sign: "If the values contained in
      *> offset-hours and offset-minutes subfields of data in a receiving field associated with an offset
      *> subformat are both zero, the position corresponding to the plus sign in the format shall contain a
      *> plus sign" — so a zero offset renders "+00:00", not "-00:00" and not "000:00":
      *>   1  +1439 = 23 h 59 m at the bound        -> "2021-06-16T12:34:56+23:59"   (legal, no EC)
      *>   2  -1439 the negative bound              -> "2021-06-16T12:34:56-23:59"   (legal, no EC)
      *>   3  0, the r7 identity's written form     -> "2021-06-16T12:34:56+00:00"   (legal, no EC)
      *>   4  +1440 is one minute past the bound  -> EC-ARGUMENT-FUNCTION
      *>   5  -1440 the negative side of it       -> EC-ARGUMENT-FUNCTION
      *>   6  RESUME AT NEXT STATEMENT abandons the failed MOVE, so the receiver still holds probe 3's value.
      *>      CAUGHT is the assertion; the untouched receiver corroborates that no returned value reached it.
      *> The bound is measured by a MATCHED PAIR one minute apart on each side: a guard at any other number
      *> would move exactly one of the four lines.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDTOFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ID  PIC 9(7) VALUE 153569.
       01 WS-SEC PIC 9(5) VALUE 45296.
       01 P25    PIC X(25) VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "1-PLUS-BOUND".
           MOVE FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss+hh:mm", WS-ID, WS-SEC, 1439) TO P25.
           DISPLAY "  [" P25 "]".
           DISPLAY "2-MINUS-BOUND".
           MOVE FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss+hh:mm", WS-ID, WS-SEC, -1439) TO P25.
           DISPLAY "  [" P25 "]".
           DISPLAY "3-ZERO".
           MOVE FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss+hh:mm", WS-ID, WS-SEC, 0) TO P25.
           DISPLAY "  [" P25 "]".
           DISPLAY "4-PLUS-OVER".
           MOVE FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss+hh:mm", WS-ID, WS-SEC, 1440) TO P25.
           DISPLAY "5-MINUS-OVER".
           MOVE FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss+hh:mm", WS-ID, WS-SEC, -1440) TO P25.
           DISPLAY "6-RECEIVER-UNTOUCHED".
           DISPLAY "  [" P25 "]".
           STOP RUN.
       END PROGRAM L1FDTOFF.
