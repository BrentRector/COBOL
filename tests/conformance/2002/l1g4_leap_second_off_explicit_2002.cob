      *> ISO §7.3.17.4 GR3 OFF specified at the 2002 floor — live-clock LAYOUT witness for three seconds positions
       >>LEAP-SECOND OFF
      *> ROLE: a LAYOUT witness only. On a live clock no line here can
      *>   fail on a seconds value > 59, so it is NOT GR3 evidence for
      *>   any source. The pins that can fail are
      *>   LeapSecondReportedSecondsPinTests (run time, incl. the 2002
      *>   specified-OFF arm) and the Unit facts
      *>   WhenCompiledStampTests.WhenCompiled_LeapSecond* (compile
      *>   time, at 2002 and 2023).
      *> GR3: "When OFF is specified or implied, a value greater than 59
      *>   shall not be reported in the seconds position of the value
      *>   returned from: the ACCEPT statement with the TIME phrase, the
      *>   CURRENT-DATE intrinsic function, the FORMATTED-CURRENT-DATE
      *>   intrinsic function, the WHEN-COMPILED intrinsic function."
      *>   cite.py --check 7.3.17.4 -> OK §7.3.17.4 3)
      *> The 2023 twin l1g4_leap_second_off_reported_seconds covers the
      *>   IMPLIED OFF (GR1) with all four sources; this one covers the
      *>   SPECIFIED OFF at --std 2002, where FORMATTED-CURRENT-DATE does
      *>   not exist (a COBOL-2014 intrinsic, IntrinsicCatalog 2014), so
      *>   the source set differs: three sources.
      *> §7.3.17.3 SR1: the directive precedes the compilation unit.
      *> Seconds positions: ACCEPT TIME 5-6 (§14.9.1.4 11)), CURRENT-DATE
      *>   13-14 (§15.21.3 1)), WHEN-COMPILED 13-14 (§15.99.3 1)); the
      *>   OFF range is "00 through 59". Live clock => SHAPE pin: each
      *>   pair is two digits in 00..59 (=> Y); position 17 is '+', '-'
      *>   or '0' (=> Y), the anchor proving the position arithmetic.
      *> Expected: T1, T2, T4 all "Y".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4LSX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TM     PIC X(8).
       01 CDT    PIC X(21).
       01 SS     PIC XX.
       01 OK-SS  PIC X.
       01 OK-SG  PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           ACCEPT TM FROM TIME
           MOVE TM(5:2) TO SS
           PERFORM CHK-SS
           DISPLAY "T1 ACCEPT TIME SS=00..59:" OK-SS
           MOVE FUNCTION CURRENT-DATE TO CDT
           MOVE CDT(13:2) TO SS
           PERFORM CHK-SS
           PERFORM CHK-SIGN
           DISPLAY "T2 CURRENT-DATE SS=00..59:" OK-SS
                   " P17-SIGN:" OK-SG
           MOVE FUNCTION WHEN-COMPILED TO CDT
           MOVE CDT(13:2) TO SS
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
           IF CDT(17:1) = "+" OR CDT(17:1) = "-" OR CDT(17:1) = "0"
               MOVE "Y" TO OK-SG
           ELSE
               MOVE "N" TO OK-SG
           END-IF.
