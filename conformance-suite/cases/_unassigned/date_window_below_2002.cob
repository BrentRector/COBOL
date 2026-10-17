*>--- conformance-case
*> id: date_window_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.100 YEAR-TO-YYYY / §15.23 DATE-TO-YYYYMMDD / §15.25 DAY-TO-YYYYDDD / §15.80
      *> SECONDS-PAST-MIDNIGHT are COBOL-2002 introductions (the Y2K windowing amendment + the clock
      *> function; PHASE-11-scout-notes.md spec:date-window). Below 2002 the D8 catalog window rejects
      *> each reference BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11DATEW85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-8 PIC 9(8).
       01 SPM PIC 9(5)V9(7).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-8 = FUNCTION YEAR-TO-YYYY(4, 23, 1995)
           COMPUTE N-8 = FUNCTION DATE-TO-YYYYMMDD(851003, 10, 2002)
           COMPUTE N-8 = FUNCTION DAY-TO-YYYYDDD(10004, 20, 2002)
           COMPUTE SPM = FUNCTION SECONDS-PAST-MIDNIGHT
           STOP RUN.
