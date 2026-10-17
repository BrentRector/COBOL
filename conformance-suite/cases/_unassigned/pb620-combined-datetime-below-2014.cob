*>--- conformance-case
*> id: pb620-combined-datetime-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:cases/15.4.1-Numeric-and-integer-functions/pb620_combined_datetime_eae — the EDITION half of the same
      *> reference that golden compiles and runs.
      *> ISO §15.17 COMBINED-DATETIME is one of the date/time functions ISO/IEC 1989:2014 added to clause 15;
      *> it is absent from ISO/IEC 1989:2002 and from ISO 1989:1985, whose intrinsic-function set contains no
      *> such function-name. A reference to it below 2014 names an undefined function and shall be rejected
      *> (COBOLNET1502, the §15 edition band), which is also what keeps the PB620 standard-mode fix from
      *> silently widening the earlier editions' surface.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB620NCD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC 9(5)V9(8) VALUE 3661.12345678.
       01 W-C PIC 9V9(13).
       PROCEDURE DIVISION.
           COMPUTE W-C = FUNCTION COMBINED-DATETIME(1, W-S + 0)
           STOP RUN.
       END PROGRAM PB620NCD.
