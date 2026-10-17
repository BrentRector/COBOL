*>--- conformance-case
*> id: formatted_datetime
*> rules: [GR-15.3.3.6.1-1, RV-15.17.4-1, RV-15.38.4-1, RV-15.39.4-1, RV-15.41.4-2, RV-15.41.4-3, RV-15.48.4-1, RV-15.79.4-1, AR-15.92.3-2, RV-15.92.4-1, RV-15.95.4-1, FMT-15.17.2, FMT-15.38.2, FMT-15.39.2, FMT-15.48.2, FMT-15.79.2, FMT-15.92.2, FMT-15.95.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.17/§15.38-§15.41/§15.48/§15.69/§15.79/§15.92/§15.95 — the COBOL-2014 date/time + number
      *> intrinsics. Integer date 153569 = 2021-06-16 (§15.5.2 epoch 1601-01-01=1); 45296 s = 12:34:56.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FMTDT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ID     PIC 9(7)  VALUE 153569.
       01 WS-SEC    PIC 9(5)  VALUE 45296.
       01 WS-CDT    PIC Z(6)9.9(5).
       01 WS-R      PIC X(30).
       01 WS-N      PIC +9(9).
       01 WS-IOFD PIC X(10).
       01 WS-F      PIC +9(5).9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATE("YYYYMMDD", WS-ID) TO WS-R.
           DISPLAY "D1=" WS-R.
           MOVE FUNCTION FORMATTED-DATE("YYYY-MM-DD", WS-ID) TO WS-R.
           DISPLAY "D2=" WS-R.
           MOVE FUNCTION FORMATTED-DATE("YYYYDDD", WS-ID) TO WS-R.
           DISPLAY "D3=" WS-R.
           MOVE FUNCTION FORMATTED-DATE("YYYY-Www-D", WS-ID) TO WS-R.
           DISPLAY "D4=" WS-R.
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss", WS-SEC) TO WS-R.
           DISPLAY "T1=" WS-R.
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ssZ", WS-SEC, 120) TO WS-R.
           DISPLAY "T2=" WS-R.
           MOVE FUNCTION FORMATTED-TIME("hhmmss+hhmm", WS-SEC, 120) TO WS-R.
           DISPLAY "T3=" WS-R.
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss", WS-ID,
               WS-SEC) TO WS-R.
           DISPLAY "DT=" WS-R.
           MOVE FUNCTION COMBINED-DATETIME(WS-ID, WS-SEC) TO WS-CDT.
           DISPLAY "CDT=" WS-CDT.
           MOVE "2021-06-16" TO WS-IOFD.
           MOVE FUNCTION INTEGER-OF-FORMATTED-DATE("YYYY-MM-DD",
               WS-IOFD) TO WS-N.
           DISPLAY "IOF=" WS-N.
           MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME("hh:mm:ss",
               "12:34:56") TO WS-N.
           DISPLAY "SFT=" WS-N.
           MOVE FUNCTION TEST-FORMATTED-DATETIME("YYYYMMDD",
               "20051314") TO WS-N.
           DISPLAY "TF1=" WS-N.
           MOVE FUNCTION TEST-FORMATTED-DATETIME("YYYYMMDD",
               "15990316") TO WS-N.
           DISPLAY "TF2=" WS-N.
           MOVE FUNCTION TEST-FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss",
               FUNCTION FORMATTED-CURRENT-DATE("YYYY-MM-DDThh:mm:ss"))
               TO WS-N.
           DISPLAY "FCD=" WS-N.
           MOVE FUNCTION NUMVAL-F("1.5E+3") TO WS-F.
           DISPLAY "NVF=" WS-F.
           MOVE FUNCTION TEST-NUMVAL-F("0 1E+2") TO WS-N.
           DISPLAY "TNF=" WS-N.
           STOP RUN.
       END PROGRAM FMTDT.
