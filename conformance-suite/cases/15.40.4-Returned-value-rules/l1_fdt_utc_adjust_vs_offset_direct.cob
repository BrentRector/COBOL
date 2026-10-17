*>--- conformance-case
*> id: l1_fdt_utc_adjust_vs_offset_direct
*> rules: [RV-15.40.4-2, RV-15.40.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.4 r2 and r3 - the two ZONE rules, run over the SAME
      *> three argument values so that the only variable is the format.
      *>   r2: "If the format in argument-1 indicates that the returned value
      *>       is to be expressed in UTC, the time portion of the returned
      *>       value reflects the adjustment of the value in argument-3 by
      *>       the offset in argument-4."
      *>   r3: "If the format in argument-1 indicates that the time is to be
      *>       returned as an offset from UTC, the value in argument-3 is
      *>       reflected directly in the time portion of the returned value,
      *>       and the offset in argument-4 is reflected directly in the
      *>       offset portion of the returned value."
      *> Nothing pinned r3 for FORMATTED-DATETIME before this file: every
      *> existing golden's four-argument call uses a UTC ('Z') format.
      *>
      *> THE DIRECTION OF r2's ADJUSTMENT IS NOT IN r2. §15.3.3.6.1 item 1
      *> supplies it - "A plus sign to indicate that the common time portion
      *> of the data is adjusted DOWNWARD by the offset values to represent
      *> UTC" - and r3 makes a positive argument-4 render with that plus
      *> sign. So a positive offset means UTC = argument-3 minus argument-4,
      *> and the UTC and OFFSET lines below are two renderings of ONE instant
      *> whose time portions must DIFFER by exactly the offset.
      *>
      *> Derivations. §15.5.2: 143951 = 1995-02-15 (no leg rolls the date -
      *> every adjusted value stays inside [0, 86400), which is why the roll
      *> and its §15.5.2 range guard are r25_utc_roll_date_range's subject
      *> and not this file's). §15.5.5: 45296 seconds past midnight is
      *> 12:34:56.
      *>   +300 min = 18000 s: 45296 - 18000 = 27296 = 07:34:56.
      *>   -300 min: 45296 + 18000 = 63296 = 17:34:56.
      *>   +345 min = 20700 s: 45296 - 20700 = 24596 = 06:49:56.
      *>   -345 min: 45296 + 20700 = 65996 = 18:19:56.
      *> §15.3.3.6.1 splits the offset into an offset-hours and an
      *> offset-minutes subfield, so 345 minutes is 05 and 45 - a case a
      *> whole-hour offset cannot distinguish from a bad division. §15.40.3
      *> r5 caps the magnitude at 1439, and its NOTE says why: "The offset
      *> value 1439 represents 23 hours 59 minutes, which is one minute less
      *> than a day" - which is exactly what the boundary lines render.
      *> The last line is Annex D.31.5.7's own worked example, with its
      *> format's four-character year restored (D.31.5.7 prints
      *> "YYMMDDThhmmss.ss+hhmm" and then a returned value beginning 1995,
      *> which §15.3.1.2's eight-character basic calendar date requires);
      *> §15.3.3.2 keeps the basic form's decimal separator out of the data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC X(40).
       01 D  PIC 9(7) VALUE 143951.
       01 S  PIC 9(5) VALUE 45296.
       01 SF PIC 9(5)V9(12) VALUE 18867.812479168304.
       PROCEDURE DIVISION.
       MAIN.
      *> r2 - a UTC format ADJUSTS the time portion, both signs.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S 300)
               TO R
           DISPLAY "UTC-P300=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S -300)
               TO R
           DISPLAY "UTC-M300=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S 345)
               TO R
           DISPLAY "UTC-P345=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S -345)
               TO R
           DISPLAY "UTC-M345=" R
      *> r3 - an OFFSET format leaves the time portion ALONE and puts the
      *> offset in the offset portion. Same arguments as the four above.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 300) TO R
           DISPLAY "OFF-P300=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S -300) TO R
           DISPLAY "OFF-M300=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 345) TO R
           DISPLAY "OFF-P345=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S -345) TO R
           DISPLAY "OFF-M345=" R
      *> §15.40.3 r5's boundary, both signs - legal, and its NOTE's 23h59m.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 1439) TO R
           DISPLAY "OFF-PMAX=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S -1439) TO R
           DISPLAY "OFF-MMAX=" R
      *> The EXTENDED offset subformat carries its own colon (§15.3.3.6.1).
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss+hh:mm"
               D S 345) TO R
           DISPLAY "OFF-E-P345=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss+hh:mm"
               D S -345) TO R
           DISPLAY "OFF-E-M345=" R
      *> Annex D.31.5.7's worked example (year field corrected per
      *> §15.3.1.2): 143951, 18867.812479168304, +300.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss.ss+hhmm"
               D SF 300) TO R
           DISPLAY "D31-5-7=" R
           STOP RUN.
