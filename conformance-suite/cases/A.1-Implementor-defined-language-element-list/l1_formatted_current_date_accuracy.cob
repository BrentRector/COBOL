      *>--- conformance-case
      *> id: l1_formatted_current_date_accuracy
      *> rules: [DOC-A.1-87]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.38.4 r2 FORMATTED-CURRENT-DATE - the ACCURACY of the
      *> time portion of the returned value (Annex A.1 item 87, which
      *> cites "Returned value rule 1"; the obligation's text is r2, and
      *> r2 is what this file cites).
      *> r2: "The implementor shall define the accuracy of the portion
      *> of the returned value that corresponds to the time format
      *> portion of the argument."  The standard states no accuracy, so
      *> the values below are derived from the implementor's own
      *> documentation (docs/CONFORMANCE.md row 87) read against the
      *> 15.3.3 format geometry, which is what an A.1 documentation item
      *> can be derived from: THE ACCURACY IS THE RUN UNIT'S CLOCK TICK,
      *> 100 ns - seven significant fraction digits, and ZEROS beyond
      *> them up to the 15.3.3.2 implementor maximum ("The implementor
      *> defines the maximum number of digit positions that may be
      *> specified in the decimal fraction portion of the seconds
      *> subfield of a time format; that maximum shall be greater than
      *> or equal to nine"), which COBOL.NET sets at 18.
      *>
      *> NO LITERAL CLOCK VALUE IS PINNED HERE, AND NONE CAN BE: the
      *> corpus runner injects no environment, so the clock is the
      *> host's. What IS deterministic is the ACCURACY CEILING, and that
      *> is the half of the determination this golden measures - at the
      *> maximum fraction width the digits BEYOND the seventh are zero
      *> on every run, for every instant. The other half, that the first
      *> seven digits are the clock tick and nothing is lost, needs an
      *> injected clock and is pinned by the unit test
      *> FormattedCurrentDate_PinnedClock_ClockTickThenZeros of the
      *> class CobolDateAccuracyTests, which renders this same 18-digit
      *> field from a fixed instant. Neither test alone verifies r2's
      *> determination; the pair does.
      *>
      *> THE GEOMETRY, character by character. 15.3.3.7: an extended
      *> combined format is an extended date, then T, then an extended
      *> time. 15.3.3.2: in an extended common time format with
      *> fractional seconds "the two colon characters and the decimal
      *> separator appear in the data". 15.3.3.6.1: the extended offset
      *> subformat is +hh:mm and "the colon character in the offset
      *> subformat of an extended offset time format appears in the
      *> data". So the 44-character value is 1..10 YYYY-MM-DD, 11 T,
      *> 12..19 hh:mm:ss, 20 the decimal separator, 21..38 the eighteen
      *> fraction digits, 39 the sign, 40..41 the offset hours, 42 a
      *> colon, 43..44 the offset minutes.
      *> TAIL11  fraction digits 8..18 = positions 28..38. Eleven zeros:
      *>         an implementation with a finer clock, or one that
      *>         padded the field with anything but zeros, fails here.
      *>         THE LEG.
      *> PAD6    positions 45..50 of a PIC X(50) receiver are spaces, so
      *>         the returned value is exactly 44 positions - the
      *>         fraction field was honoured at its full requested
      *>         width, not truncated.
      *> SEPT    the three separators the rules above place IN the data,
      *>         at 11, 20 and 42 (SEPT, SEPDOT, OFFCOL); they fix the
      *>         frame that makes TAIL11's positions mean what they say.
      *> FRAC7   digits 1..7 are digits - a class condition, not a
      *>         value: the accurate part of the field is present and
      *>         numeric.
      *> OFFSIGN 15.3.3.6.1 admits +, - or 0 in the sign position; the
      *>         clock always carries an offset, so it is + or -.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FCDACC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC X(50).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-CURRENT-DATE
               ("YYYY-MM-DDThh:mm:ss.ssssssssssssssssss+hh:mm") TO W-S
           DISPLAY "TAIL11=[" W-S(28:11) "]"
           DISPLAY "PAD6=[" W-S(45:6) "]"
           DISPLAY "SEPT=[" W-S(11:1) "]"
           DISPLAY "SEPDOT=[" W-S(20:1) "]"
           DISPLAY "OFFCOL=[" W-S(42:1) "]"
           IF W-S(21:7) IS NUMERIC
               DISPLAY "FRAC7=NUMERIC"
           ELSE
               DISPLAY "FRAC7=NOT-NUMERIC"
           END-IF
           IF W-S(39:1) = "+" OR W-S(39:1) = "-"
               DISPLAY "OFFSIGN=OK"
           ELSE
               DISPLAY "OFFSIGN=BAD"
           END-IF
           STOP RUN.
