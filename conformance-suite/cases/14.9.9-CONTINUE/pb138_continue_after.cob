*>--- conformance-case
*> id: pb138_continue_after
*> rules: [SR-14.9.9.3-1, GR-14.9.9.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB138 - CONTINUE AFTER whole. (1) The 14.6.13.1.4 nonfatal DISPATCH: a negative interval
      *> under CHECKING ON runs the matching USE declarative - BOTH handler lines print (the old site
      *> recorded the status and never dispatched; the generated handler pc was dead code) - and execution
      *> resumes (AFTER-NEG). (2) The m=0 truncation runs in the interval's OWN domain: 0.99999999999999999
      *> (17 nines - its binary64 image is exactly 1.0) suspends ZERO seconds per GR1's implicit COMPUTE
      *> without ROUNDED; the conformance runner's timeout would catch a 1-second regression, and the
      *> emitted site hands the exactly-truncated seconds beside the full-precision sign value.
      >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NEG PIC S9V9 VALUE -0.5.
       01 W-FRAC PIC 9V9(17) VALUE 0.99999999999999999.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-CONTINUE-LESS-THAN-ZERO.
       H-1.
           DISPLAY "HANDLER-A"
           CONTINUE
           DISPLAY "HANDLER-B".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN.
           CONTINUE AFTER W-NEG SECONDS
           DISPLAY "AFTER-NEG"
           CONTINUE AFTER W-FRAC SECONDS
           DISPLAY "AFTER-FRAC"
           STOP RUN.
