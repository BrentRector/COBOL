*>--- conformance-case
*> id: seconds_past_midnight
*> rules: [RV-15.80.3-1, FMT-15.80.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.80 SECONDS-PAST-MIDNIGHT - the clock-range leg split out of the 2002
      *> intrinsics_date_window golden when kb/Work R28's research fixed the introduction window: the
      *> WG4 CD 1.2 (2009) Annex D.2 item 4 lists SECONDS-PAST-MIDNIGHT among "the intrinsic functions
      *> ... introduced in this committee draft International Standard" - a 2014 function, not 2002.
      *> 15.80.3 / 15.5.5: standard numeric time form, [0, 86400) under the (only) LEAP-SECOND OFF
      *> mode. The exact value is clock-dependent - the range probe here; the pinned-clock value leg
      *> lives in the unit test (RunUnit.Clock injection).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SPMRANGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SPM PIC 9(5)V9(7).
       PROCEDURE DIVISION.
           COMPUTE SPM = FUNCTION SECONDS-PAST-MIDNIGHT
           IF SPM < 86400 DISPLAY "T12=RANGE-OK"
              ELSE DISPLAY "T12=" SPM END-IF
           STOP RUN.
