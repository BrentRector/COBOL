*>--- conformance-case
*> id: l1_fdt_rules_2014
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40 FORMATTED-DATETIME - the §15.40.2 general format and the
      *> §15.40.3 / §15.40.4 rules at the edition that INTRODUCED the
      *> function, so the "2014, 2023" span of the traceability rows is
      *> MEASURED at both ends rather than assumed from the 2023 goldens.
      *> None of these rules changed between COBOL-2014 and COBOL-2023, and
      *> this file asserts exactly that: every value here is the value its
      *> 2023 twin asserts.
      *>
      *> Rules exercised, one line group each:
      *>   §15.40.2 - FUNCTION FORMATTED-DATETIME ( argument-1 argument-2
      *>     argument-3 [ argument-4 ] ): both arities, and the §8.4.3.2.3
      *>     SR2 keyword-omitted spelling the REPOSITORY paragraph licenses.
      *>   §15.40.3 r1 - "Argument-1 shall be a national or alphanumeric
      *>     literal", with §15.40.1's type table as the observable (the
      *>     national result lands in a PIC N item).
      *>   §15.40.3 r3 - both ends of §15.5.2's integer-date interval.
      *>   §15.40.3 r4 - both ends of §15.5.5's standard-numeric-time
      *>     interval under the implied LEAP-SECOND OFF, and a fractional
      *>     value, which the form admits (§15.3.3.2).
      *>   §15.40.3 r7 - argument-4 omitted for a UTC and for an offset
      *>     format is "evaluated as though 0 were specified".
      *>   §15.40.4 r1 - the combination, across §15.3.1.1's six date forms.
      *>   §15.40.4 r2/r3 - a UTC format ADJUSTS the time portion by the
      *>     offset; an offset format shows argument-3 and argument-4 DIRECT.
      *>
      *> Derivations (all from the rule text; see the 2023 twins for the
      *> full working): §15.5.2 puts integer date 1 at 1601-01-01, so 143951
      *> is 1995-02-15, day 046 of 1995, and - by §15.3.1.7's "first week ...
      *> includes January 4" - 1995-W07-3. §15.5.5: 45296 seconds past
      *> midnight is 12:34:56, 0 is 00:00:00, 86399 is 23:59:59, and
      *> 18867.812479168304 is 05:14:27.812479168304. §15.3.3.6.1 item 1
      *> makes a positive offset an adjustment DOWNWARD to reach UTC, so
      *> +300 minutes turns 12:34:56 into 07:34:56 under a 'Z' format and
      *> leaves it alone under a '+hhmm' one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION FORMATTED-DATETIME INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC X(40).
       01 A  PIC X(40).
       01 B  PIC X(40).
       01 NR PIC N(40).
       01 D  PIC 9(7) VALUE 143951.
       01 S  PIC 9(5) VALUE 45296.
       01 SF PIC 9(5)V9(12) VALUE 18867.812479168304.
       PROCEDURE DIVISION.
       MAIN.
      *> §15.40.2 - the three-argument and four-argument forms.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D S) TO R
           DISPLAY "A3-LOCAL=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 300) TO R
           DISPLAY "A4-OFFSET=" R
      *> §8.4.3.2.3 SR2 - the licensed omission of the word FUNCTION.
           MOVE FORMATTED-DATETIME("YYYYMMDDThhmmss" D S) TO R
           DISPLAY "KOF-A3=" R
           MOVE FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 300) TO R
           DISPLAY "KOF-A4=" R
      *> §15.40.3 r1 / §15.40.1 - the national row of the type table.
           MOVE FUNCTION FORMATTED-DATETIME(N"YYYYMMDDThhmmss" D S)
               TO NR
           DISPLAY "NAT-BASIC=" NR
      *> §15.40.3 r3 - both ends of the integer date form.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 1 S) TO R
           DISPLAY "DATE-MIN=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" 3067671 S)
               TO R
           DISPLAY "DATE-MAX=" R
      *> §15.40.3 r4 - both ends of standard numeric time form, and a
      *> fractional value.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D 0) TO R
           DISPLAY "SEC-MIN=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D 86399)
               TO R
           DISPLAY "SEC-MAX=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss.ss" D SF)
               TO R
           DISPLAY "SEC-FRAC=" R
      *> §15.40.3 r7 - omitted argument-4 equals an explicit zero, for both
      *> premises of the rule.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S) TO A
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S 0)
               TO B
           DISPLAY "UTC-OMIT=" A
           IF A = B DISPLAY "UTC-OMIT-IS-ZERO=SAME"
           ELSE DISPLAY "UTC-OMIT-IS-ZERO=DIFFER" END-IF
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm" D S)
               TO A
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S 0) TO B
           DISPLAY "OFF-OMIT=" A
           IF A = B DISPLAY "OFF-OMIT-IS-ZERO=SAME"
           ELSE DISPLAY "OFF-OMIT-IS-ZERO=DIFFER" END-IF
      *> §15.40.4 r1 - the combination across the ordinal and week date
      *> forms, basic and extended.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYDDDThhmmss" D S) TO R
           DISPLAY "ORD-B=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYWwwDThhmmss" D S) TO R
           DISPLAY "WK-B=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-DDDThh:mm:ss" D S)
               TO R
           DISPLAY "ORD-E=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-Www-DThh:mm:ss" D S)
               TO R
           DISPLAY "WK-E=" R
      *> §15.40.4 r2 vs r3 - the same arguments, two zone rules.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmssZ" D S 300)
               TO R
           DISPLAY "UTC-P300=" R
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               D S -345) TO R
           DISPLAY "OFF-M345=" R
           STOP RUN.
