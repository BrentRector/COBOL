*>--- conformance-case
*> id: pb65_leap_second_on
*> rules: [GR-7.3.17.4-4, GR-7.3.17.4-6, AR-15.79.3-4, RV-15.92.4-1,
*>    DOC-A.1-112]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>LEAP-SECOND ON
      *> kb/Work PB65 (AR-15.79.3-4). ISO §7.3.17.4 GR4: with LEAP-SECOND ON
      *> "a standard numeric time form value shall be greater than or equal to
      *> zero and less than 86,401"; §15.3.3.3: the seconds subfield "shall
      *> contain a value that is greater than or equal to 00 and less than 61
      *> when the LEAP-SECOND directive with the ON phrase is in effect". So
      *> "235960" is a valid hhmmss time and §15.79.4 r1's (H*3600 + M*60 + S)
      *> is 86400; FORMATTED-TIME presents 86400 as 23:59:60; COMBINED-DATETIME
      *> accepts 86400 (§15.17.3 r2). Before this the directive was consumed
      *> and discarded: SECONDS-FROM-FORMATTED-TIME("hhmmss", "235960") answered
      *> 0 (the EC-ARGUMENT-FUNCTION default) and killed the run unit under
      *> checking. The reported side (a 60 from CURRENT-DATE etc.) is the
      *> implementor's and stays "never" (docs/CONFORMANCE.md A.1 item 111).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65LEAPON.
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
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME("hhmmss", "235961").
           DISPLAY "T4 TFD(235961)=" T.
           MOVE FUNCTION FORMATTED-TIME("hhmmss", 86400) TO S.
           DISPLAY "T5 FT(86400)=" S.
           MOVE FUNCTION FORMATTED-TIME("hhmmss.ss", 86400.5) TO S.
           DISPLAY "T6 FT(86400.5)=" S.
           COMPUTE C = FUNCTION COMBINED-DATETIME(1, 86400).
           DISPLAY "T7 CD(1 86400)=" C.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss", 1, 86400) TO S.
           DISPLAY "T8 FDT(1 86400)=" S.
           STOP RUN.
