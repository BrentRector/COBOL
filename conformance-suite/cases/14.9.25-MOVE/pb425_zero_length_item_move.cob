*>--- conformance-case
*> id: pb425_zero_length_item_move
*> rules: [GR-14.9.25.4-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §14.9.25.4 GR2's EXCLUSION and GR1's ZERO-LENGTH-ITEM clause (kb/Work PB425).
      *> This is the 2014 half of the rule only because its two probes need a DYNAMIC LENGTH item (§13.18.19,
      *> a COBOL-2014 addition); the RULE does not differ by edition.
      *>
      *> (1) THE EXCLUSION, and that it is PER RECEIVING OPERAND.  GR2 substitutes SPACE only when "the
      *>     receiving operand is other than a dynamic-length elementary item", so a dynamic-length receiver
      *>     keeps the zero-length value and its current length is 0 (§8.5.1.10.4: "If the length of the
      *>     sending operand is zero, no data is moved and the new length ... is set to zero").  One MOVE with
      *>     BOTH kinds of receiver proves the qualifier is read per receiver: LEN=0 for the dynamic-length
      *>     item AND the substituted SPACE fill for the numeric one, from one statement.
      *> (2) GR1: "If identifier-1 is a zero-length item, it is as if literal-1 were specified as a zero-length
      *>     literal" — which lands back in GR2.  A DYNAMIC LENGTH sending item at current length zero
      *>     (§8.5.4 item 4) therefore stores the SPACE fill into a numeric receiver, and the SAME item at a
      *>     non-zero length converts normally; both are shown so the runtime test is measured as a test.
      *> (3) The same route through a FUNCTION-IDENTIFIER sender (§8.5.4 item 6, "an intrinsic function that
      *>     returns a zero-length value").  FUNCTION TRIM of an all-space argument is zero-length by
      *>     §15.96.4 r4 ("If argument-1 contains only characters that are argument-2, spaces if argument-2 is
      *>     not specified, ... the returned value is of length zero"), and GR1's other sentence, "the ...
      *>     function-identifier is evaluated only once", is what lets the length be read without a second call.
      *> Expected values are computed from the rule: the SPACE fill into PIC 9(3) is three spaces — the same
      *> content MOVE SPACE TO that item deposits — and into PIC ZZ9 it is the whole-width fill, not an edit
      *> of zero.  "  77" / " 77" are the ordinary conversions, shown to prove nothing else moved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB425-ZERO-LEN-ITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-DYN   PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 R-NUM   PIC 9(3) VALUE 123.
       01 R-NED   PIC ZZ9.
       01 W-LEN   PIC 9(2).
       01 W-BLANK PIC X(4) VALUE "    ".
       01 W-FULL  PIC X(4) VALUE "  12".
       PROCEDURE DIVISION.
       MAIN.
      *> ── (1) the exclusion, per receiving operand ──
           MOVE "PRESET" TO D-DYN.
           MOVE "" TO D-DYN, R-NUM.
           MOVE FUNCTION LENGTH(D-DYN) TO W-LEN.
           DISPLAY "EXCL-LEN=" W-LEN.
           DISPLAY "EXCL-NUM=[" R-NUM "]".
      *> ── (2) GR1 through a DYNAMIC LENGTH sending item ──
           MOVE "" TO D-DYN.
           MOVE D-DYN TO R-NUM.
           MOVE D-DYN TO R-NED.
           DISPLAY "ZL-NUM=[" R-NUM "]".
           DISPLAY "ZL-NED=[" R-NED "]".
           MOVE "77" TO D-DYN.
           MOVE D-DYN TO R-NUM.
           MOVE D-DYN TO R-NED.
           DISPLAY "NZ-NUM=[" R-NUM "]".
           DISPLAY "NZ-NED=[" R-NED "]".
      *> ── (3) GR1 through a zero-length FUNCTION result ──
           MOVE FUNCTION TRIM(W-BLANK) TO R-NUM.
           DISPLAY "FN-ZL=[" R-NUM "]".
           MOVE FUNCTION TRIM(W-FULL) TO R-NUM.
           DISPLAY "FN-NZ=[" R-NUM "]".
           STOP RUN.
