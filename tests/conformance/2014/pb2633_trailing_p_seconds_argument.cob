      *> ISO 15.41.4 / 15.40.4: FORMATTED-TIME and FORMATTED-DATETIME return a
      *> representation of the seconds past midnight "contained in" the seconds
      *> argument, and 15.17.4 r1: COMBINED-DATETIME is argument-1 + (argument-2
      *> / 100000). The value of an item is the value its PICTURE gives it:
      *> PIC 9(3)PP holding 36000 stores the digits 360 and means 36000 seconds,
      *> ten o'clock, so the trailing P positions are whole seconds.
      *>
      *> kb/Work PB2633: a trailing-P item has a NEGATIVE scale and the
      *> time-function bodies divided by Pow10.AsWide(-2), which answered 1, so
      *> FORMATTED-TIME read the stored digits as seconds (00:06:00). Now:
      *>   36000 seconds        = 10:00:00
      *>   PIC 9PP VALUE 300    = 00:05:00 (stored digit 3, two trailing Ps)
      *>   integer date 1       = 1601-01-01 (15.5.2)
      *>   COMBINED-DATETIME(1, 36000) = 1 + 36000 / 100000 = 1.36
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2633TPS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC 9(3)PP VALUE 36000.
       01 S1 PIC 9PP VALUE 300.
       01 R PIC X(20).
       01 N PIC 9(3)V9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-TIME("hhmmss", S) TO R.
           DISPLAY "1-FORMATTED-TIME=" FUNCTION TRIM(R).
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss", S1) TO R.
           DISPLAY "2-FORMATTED-TIME-ONE-DIGIT=" FUNCTION TRIM(R).
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss", 1, S)
               TO R.
           DISPLAY "3-FORMATTED-DATETIME=" FUNCTION TRIM(R).
           COMPUTE N = FUNCTION COMBINED-DATETIME(1, S).
           DISPLAY "4-COMBINED-DATETIME=" N.
           STOP RUN.
