*>--- conformance-case
*> id: l1_time_fraction_max_digits
*> rules: [DOC-A.1-202]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.3.3.2 — common time formats with fractional seconds: the implementor-defined
      *> MAXIMUM number of digit positions in the decimal fraction of the seconds subfield, and
      *> the standard's floor on it (Annex A.1 item 202; docs/CONFORMANCE.md DOC-A.1-202).
      *>
      *> THE RULE. §15.3.3.2: "The implementor defines the maximum number of digit positions
      *> that may be specified in the decimal fraction portion of the seconds subfield of a time
      *> format; that maximum shall be greater than or equal to nine." The same clause fixes the
      *> two shapes measured here: an EXTENDED format with fractional seconds is "two lowercase
      *> 'h' characters ... a colon character ... two lowercase 'm' ... a colon ... two lowercase
      *> 's' ... a decimal separator; and at least one lowercase 's' character representing a
      *> digit in the decimal fraction portion", and for it "The two colon characters and the
      *> decimal separator appear in the data"; a BASIC format with fractional seconds carries
      *> the same fraction but "The decimal separator does not appear in the data associated with
      *> a basic common time format with fractional seconds representation". A period is the
      *> separator because DECIMAL-POINT IS COMMA is not specified.
      *>
      *> §15.41.3 r2 requires argument-1 of FORMATTED-TIME to BE a time format, and r3 makes
      *> argument-2 "a value in standard numeric time form" — seconds past midnight. 45296.5 is
      *> 12*3600 + 34*60 + 56 = 45296 seconds plus half a second, i.e. 12:34:56.5, and
      *> §15.41.4 r1 returns "a representation of the standard numeric time contained in
      *> argument-2 according to the format in argument-1" — the fraction rendered at the
      *> format's own width, zero-filled to the right, which is what makes every line below a
      *> derivation rather than an observation.
      *>
      *> F1  - the MINIMUM fraction the clause admits: one 's'. The 10-character extended shape.
      *> F9  - THE STANDARD'S FLOOR. "that maximum shall be greater than or equal to nine" makes
      *>       a nine-digit fraction a format that EVERY conforming implementor must accept, so
      *>       this leg is not about our determination at all: it is the conformance obligation.
      *> F18 - OUR determination, which is 18. Accepting 18 is what the documented maximum
      *>       claims; the paired negative golden conformance:cases/_unassigned/l1-time-fraction-past-max
      *>       shows 19 REJECTED, and the two together pin the maximum at exactly 18 — which
      *>       satisfies §15.3.3.2 because 18 >= 9.
      *> B9  - the BASIC shape at the same nine digits, where the separator is in the FORMAT and
      *>       not in the DATA: 15 characters against F9's 18. Without this leg a renderer that
      *>       emitted the separator everywhere would still pass.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TFR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SEC  PIC 9(5)V9(9) VALUE 45296.5.
       01 R1   PIC X(10).
       01 R9   PIC X(18).
       01 R18  PIC X(27).
       01 RB9  PIC X(15).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss.s", SEC) TO R1
           DISPLAY "F1=[" R1 "]"
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss.sssssssss", SEC) TO R9
           DISPLAY "F9=[" R9 "]"
           MOVE FUNCTION FORMATTED-TIME
               ("hh:mm:ss.ssssssssssssssssss", SEC) TO R18
           DISPLAY "F18=[" R18 "]"
           MOVE FUNCTION FORMATTED-TIME("hhmmss.sssssssss", SEC) TO RB9
           DISPLAY "B9=[" RB9 "]"
           STOP RUN.
