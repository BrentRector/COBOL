*>--- conformance-case
*> id: pb11_datetime_format_grammar
*> rules: [GR-15.3.3.6.1-1, GR-15.3.3.6.1-2, GR-15.3.3.6.1-3,
*>    AR-15.39.3-2, RV-15.39.4-1, AR-15.40.3-2, AR-15.41.3-2,
*>    AR-15.41.3-6, AR-15.48.3-2, AR-15.79.3-2, FMT-15.41.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB11 - the ISO 15.3.1-15.3.4 date/time FORMAT grammar is now RECOGNISED, not merely spot-checked.
      *> 15.39.3 r2 requires a DATE format, 15.41.3 r2 a TIME format and 15.40.3 r2 a COMBINED one. Nothing
      *> asked that question before: CobolDate.Tokenize validated character CLASSES and field WIDTHS, so any
      *> string assembled from legal subfields was accepted and the function FABRICATED a value -
      *> FORMATTED-DATE("hhmmss" ...) returned "000000".
      *>
      *> This pins the ACCEPT side: every one of the six date formats 15.3.1.1 enumerates, the common-time
      *> shapes of 15.3.3.1/15.3.3.2, the UTC and offset forms of 15.3.3.5/15.3.3.6, and both combined forms
      *> of 15.3.4. The reject side is the negative corpus (pb11-*), because a recognizer that accepts
      *> everything would pass this file and still be wrong.
      *>
      *> Integer date 100000 is 1874-10-16 (15.5.2 puts day 1 at 1601-01-01), which is day 289 of 1874 and
      *> falls in week 42 on a Friday - so the ordinal and week forms are cross-checks on each other, not three
      *> independent assertions of the same thing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB11FMTGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(22).
       PROCEDURE DIVISION.
      *> 15.3.1.2 calendar, basic and extended
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD" 100000) TO T
           DISPLAY "CAL-B=" T
           MOVE FUNCTION FORMATTED-DATE("YYYY-MM-DD" 100000) TO T
           DISPLAY "CAL-E=" T
      *> 15.3.1.4 ordinal, basic and extended
           MOVE FUNCTION FORMATTED-DATE("YYYYDDD" 100000) TO T
           DISPLAY "ORD-B=" T
           MOVE FUNCTION FORMATTED-DATE("YYYY-DDD" 100000) TO T
           DISPLAY "ORD-E=" T
      *> 15.3.1.6 week, basic and extended
           MOVE FUNCTION FORMATTED-DATE("YYYYWwwD" 100000) TO T
           DISPLAY "WK-B=" T
           MOVE FUNCTION FORMATTED-DATE("YYYY-Www-D" 100000) TO T
           DISPLAY "WK-E=" T
      *> 15.3.3.1/15.3.3.2 common time: integer and fractional seconds, basic and extended
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 3600) TO T
           DISPLAY "T-B=" T
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss.ss" 3600) TO T
           DISPLAY "T-EF=" T
      *> 15.3.3.5 UTC, 15.3.3.6 offset (extended offset subformat pairs with the extended common part)
           MOVE FUNCTION FORMATTED-TIME("hhmmssZ" 3600) TO T
           DISPLAY "T-UTC=" T
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss+hh:mm" 3600 60) TO T
           DISPLAY "T-OFF=" T
      *> 15.3.4 combined: basic with basic, extended with extended
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 100000 3600) TO T
           DISPLAY "DT-B=" T
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss" 100000 3600) TO T
           DISPLAY "DT-E=" T
      *> ---- THE VALUE RULES (PB11's second half) ----
      *> 15.41.3 r6 / 15.40.3 r7 - the offset argument may be OMITTED for a UTC or offset format, and the
      *> function is then "evaluated as though 0 were specified". This is the CONVERSE of r5/r6 and is LEGAL;
      *> the COBOLNET1633 screen is deliberately one-sided so it cannot reject these.
           MOVE FUNCTION FORMATTED-TIME("hhmmss+hhmm" 3600) TO T
           DISPLAY "OMIT-OFF=" T
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" 100000 3600) TO T
           DISPLAY "OMIT-DT-UTC=" T
      *> 15.41.3 r4 / 15.40.3 r5 - the magnitude of the offset shall be <= 1439. The BOUNDARY is legal, and the
      *> NOTE explains why it is that number: 1439 minutes is 23 hours 59 minutes, one minute less than a day -
      *> which is exactly what it renders as.
           MOVE FUNCTION FORMATTED-TIME("hhmmss+hhmm" 3600 1439) TO T
           DISPLAY "OFF-MAX=" T
           MOVE FUNCTION FORMATTED-TIME("hhmmss+hhmm" 3600 -1439) TO T
           DISPLAY "OFF-MIN=" T
      *> 15.41.3 r3 / 15.40.3 r4 - the seconds argument shall be a value in STANDARD NUMERIC TIME FORM, whose
      *> range 7.3.17 r5 fixes at 0 <= v < 86,400 under LEAP-SECOND OFF (the only mode this compiler supports).
      *> 86399 is the last legal value; 100000 seconds is about 27.7 hours and used to FABRICATE hh=27 with no
      *> exception condition. It now takes the 15.3 EC-ARGUMENT-FUNCTION default result.
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 86399) TO T
           DISPLAY "SEC-MAX=" T
           MOVE SPACES TO T
           MOVE FUNCTION FORMATTED-TIME("hhmmss" 100000) TO T
           DISPLAY "SEC-OVER=[" T "]"
           STOP RUN.
