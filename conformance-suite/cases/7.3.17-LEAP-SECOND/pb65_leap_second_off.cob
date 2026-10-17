*>--- conformance-case
*> id: pb65_leap_second_off
*> rules: [GR-7.3.17.4-5, GR-7.3.17.4-7, AR-15.79.3-4, RV-15.92.4-1,
*>    DOC-A.1-112]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>LEAP-SECOND OFF
      *> kb/Work PB65 — the OFF twin of pb65_leap_second_on (§7.3.17.4 GR1: OFF
      *> is the implied default; GR5: standard numeric time form is [0, 86,400);
      *> §15.3.3.3's seconds subfield is 00..59). "235960" is invalid — an
      *> EC-ARGUMENT-FUNCTION and the §15.3 default returned value (0 / spaces),
      *> and TEST-FORMATTED-DATETIME reports position 5 (the first character at
      *> which the error can be determined — a '6' in the tens of seconds).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65LEAPOFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC 9(6)V99.
       01 T  PIC 99.
       01 S  PIC X(15).
       01 C  PIC 9(7)V9(5).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960").
           DISPLAY "T1 SFFT(235960)=" R.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME("hhmmss", "235959").
           DISPLAY "T2 SFFT(235959)=" R.
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME("hhmmss", "235960").
           DISPLAY "T3 TFD(235960)=" T.
           MOVE FUNCTION FORMATTED-TIME("hhmmss", 86400) TO S.
           DISPLAY "T5 FT(86400)=[" S "]".
           MOVE FUNCTION FORMATTED-TIME("hhmmss", 86399) TO S.
           DISPLAY "T6 FT(86399)=[" S "]".
           COMPUTE C = FUNCTION COMBINED-DATETIME(1, 86400).
           DISPLAY "T7 CD(1 86400)=" C.
           STOP RUN.
