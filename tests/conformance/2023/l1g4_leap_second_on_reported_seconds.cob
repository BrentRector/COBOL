      *> ISO §7.3.17.4 GR2 under >>LEAP-SECOND ON — live-clock LAYOUT witness for the four seconds positions
       >>LEAP-SECOND ON
      *> ROLE: a LAYOUT witness only (§14.9.1.4 11), §15.21.3 1),
      *>   §15.3.3.7, §15.99.3 1) position arithmetic under ON). On a
      *>   live clock no line here can fail on a seconds value > 59, so
      *>   it is NOT the GR2 / A.1 item 111 evidence. The pins that can
      *>   fail are LeapSecondReportedSecondsPinTests (run time, clock
      *>   at 2016-12-31T23:59:59.9999999+00:00) and the Unit facts
      *>   WhenCompiledStampTests.WhenCompiled_LeapSecond* (compile
      *>   time, IntrinsicBinder.CompileClock at the same instant).
      *> GR2: "When ON is specified or implied, the implementor defines
      *>   whether a value greater than 59 may be reported in the seconds
      *>   position of the value returned from: the ACCEPT statement with
      *>   the TIME phrase, the CURRENT-DATE intrinsic function, the
      *>   FORMATTED-CURRENT-DATE intrinsic function, the WHEN-COMPILED
      *>   intrinsic function."  cite.py --check 7.3.17.4 -> OK 2)
      *> A.1 111): "LEAP-SECOND directive (whether a value greater than 59
      *>   seconds may be reported and, if so, the maximum number of
      *>   seconds that may be reported). This item is required."
      *>   cite.py --check A.1 -> OK §A.1 111)
      *> The implementor's determination (docs/CONFORMANCE.md DOC-A.1-111):
      *>   NO - with ON in effect the seconds position never holds a value
      *>   greater than 59, so the maximum is 59. §15.21.3 / §15.99.3 /
      *>   §14.9.1.4 11) give the ON range as "00 through nn, where nn is
      *>   defined by the implementor"; nn = 59 here.
      *> §7.3.17.3 SR1: the directive precedes the compilation unit.
      *> Seconds positions: ACCEPT TIME 5-6 (§14.9.1.4 11)), CURRENT-DATE
      *>   13-14 (§15.21.3 1)), FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss")
      *>   14-15 (§15.3.3.7 basic combined format), WHEN-COMPILED 13-14
      *>   (§15.99.3 1)). The clock is live, so the golden pins SHAPE:
      *>   each pair is two digits in 00..nn = 00..59 (=> Y) plus the
      *>   layout anchors (position 17 '+'/'-'/'0'; position 9 'T').
      *> The deterministic boundary pin (clock at 23:59:59.9999999 under
      *>   ON) is LeapSecondReportedSecondsPinTests in the Conformance
      *>   assembly; a file golden cannot pin the clock.
      *> Expected: T1..T4 all "Y", T3 P9 "T".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G4LSN.
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
