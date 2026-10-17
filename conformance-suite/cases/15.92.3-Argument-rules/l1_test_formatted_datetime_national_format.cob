      *>--- conformance-case
      *> id: l1_test_formatted_datetime_national_format
      *> rules: [AR-15.92.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.92.3 1) — TEST-FORMATTED-DATETIME, national argument-1
      *> with the alphanumeric arm beside it as the control.
      *>
      *> "Argument-1 shall be a national or alphanumeric literal."
      *> 8.3.3.5.1 - "National literals are of the class and category
      *> national" - makes N"YYYYMMDD" exactly the literal the rule's
      *> first word admits. 15.92.3 2) additionally requires that
      *> "Argument-2 shall be of the same type as argument-1", so the
      *> national spelling is driven against a national argument-2
      *> (PIC N) and the alphanumeric spelling against PIC X; that
      *> pair is the whole of what r1 admits.
      *>
      *> THE ANSWERS ARE THE STANDARD'S OWN, from the 15.92.4 NOTE,
      *> and the derivation is 15.92.4 1): "If no format problems or
      *> range problems occur ... the value returned is zero.
      *> Otherwise, the value returned is the ordinal character
      *> position at which the first error in argument-2 was
      *> detected."
      *>   "20051314" under YYYYMMDD answers 6. 15.3.1.3: "The month
      *>   subfield of the data (corresponding to 'MM' in the format)
      *>   shall contain a value from 01 through 12 inclusive." The
      *>   month occupies positions 5-6 and holds 13; position 5
      *>   alone ('1') is still consistent with 10, 11 and 12, so
      *>   position 6 ('3') is the first character at which the
      *>   violation is decidable.
      *>   "15990316" answers 2. 15.3.1.3: "The year subfield of the
      *>   data ... shall contain a value greater than 1600 and less
      *>   than or equal to 9999." Position 1 ('1') is still
      *>   consistent with 1601..1999; position 2 ('5') already fixes
      *>   the year below 1600 whatever follows.
      *>   "20210616" answers 0: year 2021 is greater than 1600,
      *>   month 06 is in 01..12, and 15.3.1.3's day-of-month rule
      *>   admits 16 for a 30-day month.
      *> The ANU- row repeats the first case through the alphanumeric
      *> arm, so a divergence between the two is a defect in one
      *> channel rather than a property of the rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TFD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ND1 PIC N(8) VALUE N"20051314".
       01 ND2 PIC N(8) VALUE N"15990316".
       01 ND3 PIC N(8) VALUE N"20210616".
       01 AD1 PIC X(8) VALUE "20051314".
       01 T   PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME(N"YYYYMMDD" ND1)
           DISPLAY "NAT-BADMONTH=" T
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME(N"YYYYMMDD" ND2)
           DISPLAY "NAT-BADYEAR=" T
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME(N"YYYYMMDD" ND3)
           DISPLAY "NAT-VALID=" T
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME("YYYYMMDD" AD1)
           DISPLAY "ANU-BADMONTH=" T
           STOP RUN.
       END PROGRAM L1TFD01.
