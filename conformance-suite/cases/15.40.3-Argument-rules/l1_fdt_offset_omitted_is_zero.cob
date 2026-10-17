*>--- conformance-case
*> id: l1_fdt_offset_omitted_is_zero
*> rules: [AR-15.40.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r7 - "If argument-4 is omitted and the time portion of
      *> the format in argument-1 is a UTC format or an offset format, the
      *> function shall be evaluated as though 0 were specified for
      *> argument-4."
      *>
      *> THE RULE IS AN EQUIVALENCE AND HAS TWO PREMISES, so it is measured
      *> as an equivalence over both: for each of the four format shapes the
      *> premise admits - basic UTC, extended UTC, basic offset, extended
      *> offset (§15.3.3.5, §15.3.3.6) - the omitted form and the explicit
      *> zero form are computed into two separate items and COMPARED. A
      *> DIFFER on any line is the failure, and printing both values keeps a
      *> pair that agreed by both being blank from reading as a pass.
      *>
      *> This is the CONVERSE of §15.40.3 r6, whose rejection is pinned by
      *> negative/pb11-offset-arg-local-format-datetime (COBOLNET1633): an
      *> offset argument is barred only for a LOCAL time portion, and
      *> omitting it for a UTC or offset portion is expressly legal.
      *>
      *> Derivations. §15.5.2: 143951 = 1995-02-15. §15.5.5: 45296 seconds
      *> past midnight = 12:34:56. §15.40.4 r2: a UTC format shows
      *> argument-3 adjusted by argument-4, and an adjustment by zero is the
      *> value itself, so the time portion stays 12:34:56 and §15.3.3.5
      *> appends the Z. §15.40.4 r3: an offset format shows argument-4
      *> directly in the offset portion, and §15.3.3.6.1 requires that when
      *> the offset-hours and offset-minutes subfields are both zero "the
      *> position corresponding to the plus sign in the format shall contain
      *> a plus sign" - its NOTE 3 spells the two renderings out, +0000 for
      *> the basic subformat and +00:00 for the extended one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(40).
       01 B PIC X(40).
       01 D PIC 9(7) VALUE 143951.
       01 S PIC 9(5) VALUE 45296.
       PROCEDURE DIVISION.
       MAIN.
      *> §15.3.3.5 - a BASIC UTC time portion.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S) TO A
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S 0)
               TO B
           DISPLAY "UTC-B-OMIT=" A
           DISPLAY "UTC-B-ZERO=" B
           IF A = B DISPLAY "UTC-B=SAME" ELSE DISPLAY "UTC-B=DIFFER"
           END-IF
      *> §15.3.3.5 - an EXTENDED UTC time portion.
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ssZ" D S)
               TO A
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ssZ"
               D S 0) TO B
           DISPLAY "UTC-E-OMIT=" A
           DISPLAY "UTC-E-ZERO=" B
           IF A = B DISPLAY "UTC-E=SAME" ELSE DISPLAY "UTC-E=DIFFER"
           END-IF
      *> §15.3.3.6 - a BASIC offset time portion.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm" D S)
               TO A
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 0) TO B
           DISPLAY "OFF-B-OMIT=" A
           DISPLAY "OFF-B-ZERO=" B
           IF A = B DISPLAY "OFF-B=SAME" ELSE DISPLAY "OFF-B=DIFFER"
           END-IF
      *> §15.3.3.6 - an EXTENDED offset time portion.
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss+hh:mm"
               D S) TO A
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss+hh:mm"
               D S 0) TO B
           DISPLAY "OFF-E-OMIT=" A
           DISPLAY "OFF-E-ZERO=" B
           IF A = B DISPLAY "OFF-E=SAME" ELSE DISPLAY "OFF-E=DIFFER"
           END-IF
           STOP RUN.
