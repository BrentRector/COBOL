      *> ISO §7.3.17.4 GR3 (+GR1) implied OFF — live-clock LAYOUT witness for the four seconds positions
      *> ROLE: a LAYOUT witness only. On a live clock no line here can
      *>   fail on a seconds value > 59, so it is NOT GR3 evidence for
      *>   any source. The pins that can fail are
      *>   LeapSecondReportedSecondsPinTests (run time, clock at
      *>   2016-12-31T23:59:59.9999999+00:00) and the Unit facts
      *>   WhenCompiledStampTests.WhenCompiled_LeapSecond* (compile
      *>   time, IntrinsicBinder.CompileClock at the same instant).
      *> GR1: "If the LEAP-SECOND directive is not specified, a LEAP-SECOND
      *>   directive with the OFF phrase is implied before the first
      *>   compilation unit in the compilation group."
      *>   cite.py --check 7.3.17.4 -> OK §7.3.17.4 1)
      *> GR3: "When OFF is specified or implied, a value greater than 59
      *>   shall not be reported in the seconds position of the value
      *>   returned from: the ACCEPT statement with the TIME phrase, the
      *>   CURRENT-DATE intrinsic function, the FORMATTED-CURRENT-DATE
      *>   intrinsic function, the WHEN-COMPILED intrinsic function."
      *>   cite.py --check 7.3.17.4 -> OK §7.3.17.4 3)
      *> Seconds positions, each from its own layout rule:
      *>   ACCEPT TIME   5-6 of HHMMSShh  (§14.9.1.4 11), moved per MOVE
      *>                 rules to PIC X(8) by §14.9.1.4 6))
      *>   CURRENT-DATE  13-14 of 21      (§15.21.3 1))
      *>   FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss") 14-15: a basic
      *>                 combined format, date(8) 'T'(1) hhmmss
      *>                 (§15.3.3.7, §15.3.3.1, §15.38.4 1))
      *>   WHEN-COMPILED 13-14 of 21      (§15.99.3 1))
      *> The clock is live, so the golden pins SHAPE: each seconds pair is
      *> two digits in 00..59 (=> Y), plus a layout anchor proving the
      *> position arithmetic: position 17 of CURRENT-DATE / WHEN-COMPILED
      *> is '+', '-' or '0' (§15.21.3 / §15.99.3) and position 9 of the
      *> formatted value is the separator 'T' (§15.3.3.7).
      *> Expected: T1..T4 all "Y", T3 P9 "T".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4LSF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TM     PIC X(8).
       01 CD     PIC X(21).
       01 FCD    PIC X(15).
       01 SS     PIC XX.
       01 OK-SS  PIC X.
       01 OK-SG  PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT TM FROM TIME
           MOVE TM(5:2) TO SS
           PERFORM CHK-SS
           DISPLAY "T1 ACCEPT TIME SS=00..59:" OK-SS
           MOVE FUNCTION CURRENT-DATE TO CD
           MOVE CD(13:2) TO SS
           PERFORM CHK-SS
           PERFORM CHK-SIGN
           DISPLAY "T2 CURRENT-DATE SS=00..59:" OK-SS
                   " P17-SIGN:" OK-SG
           MOVE FUNCTION FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss")
             TO FCD
           MOVE FCD(14:2) TO SS
           PERFORM CHK-SS
           DISPLAY "T3 FORMATTED-CURRENT-DATE SS=00..59:" OK-SS
                   " P9:" FCD(9:1)
           MOVE FUNCTION WHEN-COMPILED TO CD
           MOVE CD(13:2) TO SS
           PERFORM CHK-SS
           PERFORM CHK-SIGN
           DISPLAY "T4 WHEN-COMPILED SS=00..59:" OK-SS
                   " P17-SIGN:" OK-SG
           STOP RUN.
       CHK-SS.
           IF SS IS NUMERIC AND SS NOT > "59"
               MOVE "Y" TO OK-SS
           ELSE
               MOVE "N" TO OK-SS
           END-IF.
       CHK-SIGN.
           IF CD(17:1) = "+" OR CD(17:1) = "-" OR CD(17:1) = "0"
               MOVE "Y" TO OK-SG
           ELSE
               MOVE "N" TO OK-SG
           END-IF.
