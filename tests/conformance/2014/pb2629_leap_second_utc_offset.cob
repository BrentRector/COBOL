       >>LEAP-SECOND ON
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
      *> kb/Work PB2629 - FORMATTED-TIME / FORMATTED-DATETIME under
      *> >>LEAP-SECOND ON with a UTC format: an OMITTED offset and a
      *> written offset of 0 are one value, and the leap second keeps
      *> its place as second 60 whatever the offset.
      *>
      *> THE RULES. 7.3.17.4 GR4: under ON a standard numeric time form
      *> value is >= 0 and < 86,401, so 86400.5 is the day's leap
      *> second, 23:59:60.5 (15.3.3.3: the seconds subfield is < 61).
      *> 15.41.3 r6 / 15.40.3 r7: an omitted offset with a UTC format is
      *> evaluated "as though 0 were specified". 15.41.4 r2/15.40.4 r2:
      *> a UTC format shows the time adjusted by the offset (local time
      *> minus the offset); r3: an offset format shows it directly.
      *>
      *> THE DEFECT. The omitted offset skipped the adjustment, showing
      *> 23:59:60.5Z, while a written 0 ran the day roll, which read the
      *> leap second as 00:00:00.5 of the NEXT day (FORMATTED-DATETIME
      *> advanced the date, and at the last integer date raised
      *> EC-ARGUMENT-FUNCTION on a legal argument list). Any offset
      *> folded the leap second into an ordinary second.
      *>
      *> EXPECTED, from the rules: offset 0 (omitted, literal, item) is
      *> 23:59:60.5Z; +60 minutes is 22:59:60.5Z; -60 is 00:59:60.5Z
      *> (the date moves to the next day); the offset format shows
      *> 23:59:60.5+01:00. Integer date 1 is 1601-01-01 and 3,067,671 is
      *> 9999-12-31 (15.5.2). EC-ARGUMENT-FUNCTION CHECKING is ON, so a
      *> raised exception would end the run before the last line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2629LS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC 9(5)V9 VALUE 86400.5.
       01 Z0 PIC S9(4) VALUE 0.
       01 P  PIC S9(4) VALUE 60.
       01 M  PIC S9(4) VALUE -60.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1-T-OMIT="
               FUNCTION FORMATTED-TIME("hh:mm:ss.sZ", S)
           DISPLAY "2-T-ZERO="
               FUNCTION FORMATTED-TIME("hh:mm:ss.sZ", S, 0)
           DISPLAY "3-T-ITEM0="
               FUNCTION FORMATTED-TIME("hh:mm:ss.sZ", S, Z0)
           DISPLAY "4-T-PLUS60="
               FUNCTION FORMATTED-TIME("hh:mm:ss.sZ", S, P)
           DISPLAY "5-T-MINUS60="
               FUNCTION FORMATTED-TIME("hh:mm:ss.sZ", S, M)
           DISPLAY "6-T-OFFSETFMT="
               FUNCTION FORMATTED-TIME("hh:mm:ss.s+hh:mm", S, P)
           IF FUNCTION FORMATTED-TIME("hhmmssZ", S)
              = FUNCTION FORMATTED-TIME("hhmmssZ", S, 0)
               DISPLAY "7-R6-SAME=OK"
           ELSE
               DISPLAY "7-R6-SAME=BAD"
           END-IF
           DISPLAY "8-D-OMIT=" FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss.sZ", 1, S)
           DISPLAY "9-D-ZERO=" FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss.sZ", 1, S, 0)
           DISPLAY "10-D-MINUS60=" FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss.sZ", 1, S, M)
           DISPLAY "11-D-LAST-ZERO=" FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss.sZ", 3067671, S, 0)
           DISPLAY "12-D-LAST-OMIT=" FUNCTION FORMATTED-DATETIME(
               "YYYY-MM-DDThh:mm:ss.sZ", 3067671, S)
           STOP RUN.
