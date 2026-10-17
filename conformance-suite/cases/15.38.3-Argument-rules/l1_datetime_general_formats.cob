*>--- conformance-case
*> id: l1_datetime_general_formats
*> rules: [AR-15.38.3-1, RV-15.38.4-1, AR-15.39.3-3, FMT-15.17.2,
*>    FMT-15.38.2, FMT-15.39.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.17.2 / §15.38.2 / §15.39.2 general format — the printed FUNCTION <name> ( argument … ) forms of
      *> COMBINED-DATETIME, FORMATTED-CURRENT-DATE and FORMATTED-DATE, plus the argument and returned-value rules
      *> that ride on them. Every expected value below is derived from the standard's own rule text; nothing is
      *> copied from what the compiler emits, and no clock reading is written down as a literal expectation.
      *>
      *> WHICH LINE PINS WHICH INVENTORY ROW
      *>
      *>   CDT=                     FMT-15.17.2. The format is
      *>                            "FUNCTION COMBINED-DATETIME ( argument-1 argument-2 )" — both words
      *>                            UNDERLINED, therefore required, and two unbracketed arguments, therefore
      *>                            exactly two. §15.17.4 r1 gives the equivalent arithmetic expression
      *>                            "argument-1 + (argument-2 / 100000)", so 143951 + 45296/100000 =
      *>                            143951.45296, which a PIC 9(7)V9(5) receiver renders as 014395145296.
      *>                            §15.17.3 r1/r2 are satisfied: 143951 is in integer date form (§15.5.2)
      *>                            and 45296 is in standard numeric time form (§15.5.5).
      *>
      *>   FD-MIN= / FD-MAX=        FMT-15.39.2 (the printed two-argument form) and AR-15.39.3-3
      *>                            ("Argument-2 shall be a value in integer date form"). §15.5.2 fixes that
      *>                            form's interval: the starting date is Monday, 1 January 1601 — so integer
      *>                            date 1 IS 1601-01-01 — and a value "shall be greater than zero and shall
      *>                            be less than or equal to the value of FUNCTION INTEGER-OF-DATE (99991231),
      *>                            which is 3,067,671", so 3067671 IS 9999-12-31. §15.3.1.2's basic calendar
      *>                            date format is YYYYMMDD, so §15.39.4 r1 renders 16010101 and 99991231.
      *>                            Both ENDS are here deliberately: a guard written as an upper-bound test
      *>                            passes a one-sided probe (kb/Work PB22's low-end lesson).
      *>
      *>   FCD-VALID=               FMT-15.38.2 (one argument — the format prints "( argument-1 )"),
      *>                            AR-15.38.3-1's alphanumeric-literal arm, and RV-15.38.4-1's SECOND
      *>                            sentence, "The returned value is formatted according to the format in
      *>                            argument-1". §15.92.4 r1: TEST-FORMATTED-DATETIME returns zero "if no
      *>                            format problems or range problems occur during the evaluation of
      *>                            argument-2 according to the format in argument-1", so a correctly
      *>                            formatted current date and time answers 00 whatever the clock says.
      *>                            §15.38.3 r2 requires a COMBINED date and time format, which
      *>                            YYYY-MM-DDThh:mm:ss is (§15.3.3.7).
      *>
      *>   FCD-SEPARATORS=          RV-15.38.4-1's second sentence at the character level. §15.3.1.2: in the
      *>                            extended calendar date format "The two hyphens appear in the data";
      *>                            §15.3.3.7: the uppercase 'T' "appears in the data associated with the
      *>                            format"; §15.3.3.1: in the extended common time format "The two colon
      *>                            characters appear in the data". Counting the format's own characters,
      *>                            YYYY-MM-DDThh:mm:ss puts '-' at 5 and 8, 'T' at 11, ':' at 14 and 17.
      *>
      *>   FCD-TRACKS-SYSTEM-DATE=  RV-15.38.4-1's FIRST sentence, DATE half — "a representation of the
      *>   FCD-TRACKS-SYSTEM-CLOCK= current date AND TIME provided by the system on which the function is
      *>                            evaluated". The sentence names TWO things, so it takes TWO assertions:
      *>                            an implementation returning today's date with a FROZEN time (say always
      *>                            000000) satisfies FCD-VALID, FCD-SEPARATORS and the date comparison
      *>                            alone, and §15.38.4 r2 does NOT close that hole — r2 is the ACCURACY of
      *>                            the time portion (implementor-defined), not whether the time comes from
      *>                            the system at all.
      *>                            DATE: §15.21.3 r1 makes CURRENT-DATE character positions 1-4, 5-6 and
      *>                            7-8 the year, month and day of the same system, so both values convert
      *>                            through INTEGER-OF-FORMATTED-DATE and the difference is 0 or 1.
      *>                            CLOCK: positions 10-15 of a YYYYMMDDThhmmss value are its basic common
      *>                            time format portion (§15.3.3.7 — the uppercase 'T' at position 9
      *>                            "appears in the data"; §15.3.3.1 — "The basic common time format with
      *>                            integer seconds representation contains six characters"), and §15.21.3
      *>                            r1 makes CURRENT-DATE positions 9-10 / 11-12 / 13-14 the hours, minutes
      *>                            and seconds of the same system. §15.79.3 r1/r3 admit an alphanumeric
      *>                            format literal with a same-type PIC X(6) argument-2 (r3 is "shall have
      *>                            the same TYPE" — no data-item-only restriction, unlike §15.48.3 r3), and
      *>                            §15.79.4 r1's equivalent arithmetic expression ((H * 3600) + (M * 60)
      *>                            + S) makes each value seconds past midnight.
      *>                            BOTH BOUNDS ARE DERIVED, NOT OBSERVED. The two reads are sequential, so
      *>                            the second cannot precede the first: the day difference cannot be
      *>                            negative and can exceed zero only by the single day a midnight rollover
      *>                            adds, and the elapsed-seconds difference cannot be negative and is
      *>                            bounded by the run time between two adjacent statements. The 60-second
      *>                            slack is slack, not a fudge — a constant, a fabricated value or a frozen
      *>                            clock fails the test at an arbitrary run instant, which an equality
      *>                            could not do race-free. No clock value appears in the expected output.
      *>
      *>   FCD-NAT-SEPARATORS=      AR-15.38.3-1's OTHER arm — "a national or alphanumeric literal" — and with
      *>                            it §15.38.1's National row ("Argument type National → Function type
      *>                            National"). §15.26 DISPLAY-OF converts the national result back so the
      *>                            same §15.3.1.2 / §15.3.3.7 / §15.3.3.1 separator positions can be read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DTFMT1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-CDT   PIC 9(7)V9(5).
       01 W-FD8   PIC X(8).
       01 W-DT15  PIC X(15).
       01 W-DT19  PIC X(19).
       01 W-ND19  PIC N(19).
       01 W-CUR   PIC X(21).
       01 W-CD8   PIC X(8).
       01 W-TM6   PIC X(6).
       01 W-T     PIC 9(2).
       01 W-I1    PIC S9(9).
       01 W-I2    PIC S9(9).
       01 W-DIF   PIC S9(9).
       01 W-S1    PIC S9(9).
       01 W-S2    PIC S9(9).
       01 W-EL    PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
      *> FMT-15.17.2 — the printed two-argument form; §15.17.4 r1's EAE.
           COMPUTE W-CDT = FUNCTION COMBINED-DATETIME ( 143951 45296 )
           DISPLAY "CDT=" W-CDT
      *> FMT-15.39.2 + AR-15.39.3-3 — both ends of §15.5.2's interval.
           MOVE FUNCTION FORMATTED-DATE ( "YYYYMMDD" 1 ) TO W-FD8
           DISPLAY "FD-MIN=" W-FD8
           MOVE FUNCTION FORMATTED-DATE ( "YYYYMMDD" 3067671 ) TO W-FD8
           DISPLAY "FD-MAX=" W-FD8
      *> FMT-15.38.2 + AR-15.38.3-1 + RV-15.38.4-1 (second sentence).
           MOVE FUNCTION FORMATTED-CURRENT-DATE (
               "YYYY-MM-DDThh:mm:ss" ) TO W-DT19
           COMPUTE W-T = FUNCTION TEST-FORMATTED-DATETIME (
               "YYYY-MM-DDThh:mm:ss" W-DT19 )
           DISPLAY "FCD-VALID=" W-T
           IF W-DT19(5:1) = "-" AND W-DT19(8:1) = "-"
               AND W-DT19(11:1) = "T"
               AND W-DT19(14:1) = ":" AND W-DT19(17:1) = ":"
               DISPLAY "FCD-SEPARATORS=OK"
           ELSE
               DISPLAY "FCD-SEPARATORS=BAD"
           END-IF
      *> RV-15.38.4-1 (first sentence) — the value follows the SYSTEM clock,
      *> in BOTH of the things that sentence names: the date AND the time.
           MOVE FUNCTION FORMATTED-CURRENT-DATE (
               "YYYYMMDDThhmmss" ) TO W-DT15
           MOVE W-DT15(1:8) TO W-FD8
           MOVE W-DT15(10:6) TO W-TM6
           COMPUTE W-S1 = FUNCTION SECONDS-FROM-FORMATTED-TIME (
               "hhmmss" W-TM6 )
           COMPUTE W-I1 = FUNCTION INTEGER-OF-FORMATTED-DATE (
               "YYYYMMDD" W-FD8 )
           MOVE FUNCTION CURRENT-DATE TO W-CUR
           MOVE W-CUR(1:8) TO W-CD8
           MOVE W-CUR(9:6) TO W-TM6
           COMPUTE W-S2 = FUNCTION SECONDS-FROM-FORMATTED-TIME (
               "hhmmss" W-TM6 )
           COMPUTE W-I2 = FUNCTION INTEGER-OF-FORMATTED-DATE (
               "YYYYMMDD" W-CD8 )
           COMPUTE W-DIF = W-I2 - W-I1
           IF W-DIF = 0 OR W-DIF = 1
               DISPLAY "FCD-TRACKS-SYSTEM-DATE=YES"
           ELSE
               DISPLAY "FCD-TRACKS-SYSTEM-DATE=NO"
           END-IF
           COMPUTE W-EL = (W-I2 - W-I1) * 86400 + W-S2 - W-S1
           IF W-EL >= 0 AND W-EL <= 60
               DISPLAY "FCD-TRACKS-SYSTEM-CLOCK=YES"
           ELSE
               DISPLAY "FCD-TRACKS-SYSTEM-CLOCK=NO"
           END-IF
      *> AR-15.38.3-1 — the NATIONAL literal arm; §15.38.1's National row.
           MOVE FUNCTION FORMATTED-CURRENT-DATE (
               N"YYYY-MM-DDThh:mm:ss" ) TO W-ND19
           MOVE FUNCTION DISPLAY-OF ( W-ND19 ) TO W-DT19
           IF W-DT19(5:1) = "-" AND W-DT19(11:1) = "T"
               AND W-DT19(14:1) = ":"
               DISPLAY "FCD-NAT-SEPARATORS=OK"
           ELSE
               DISPLAY "FCD-NAT-SEPARATORS=BAD"
           END-IF
           STOP RUN.
       END PROGRAM L1DTFMT1.
