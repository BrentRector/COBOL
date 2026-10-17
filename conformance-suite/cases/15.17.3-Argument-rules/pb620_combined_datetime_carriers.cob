*>--- conformance-case
*> id: pb620_combined_datetime_carriers
*> rules: [AR-15.17.3-1, AR-15.17.3-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB620 — ISO §15.17 COMBINED-DATETIME under NATIVE arithmetic: the twin of
      *> conformance:2014/pb620_combined_datetime_eae, and the only place the THIRD carrier is reachable.
      *>
      *> WHY A SECOND PROGRAM. §15.17.3 r2 is a rule about argument-2's VALUE — "Argument-2 shall be in standard
      *> numeric time form", which §15.5.5 defines by magnitude alone — so the USAGE of the item holding that
      *> value cannot decide whether §15.3's "incorrect value for that argument" applies. COMBINED-DATETIME has
      *> three carriers: the exact fixed-point lane, the SDIDI (which claims every floating-point argument under
      *> ARITHMETIC IS STANDARD-DECIMAL, so the binary64 body is unreachable there), and binary64 under NATIVE.
      *> The binary64 body had NO argument rule at all: measured before the fix, F3 below computed
      *> 1.8639999999999 and set no exception condition, while the identical value in a fixed-point item
      *> terminated the run unit in the same program under the same armed directive.
      *>
      *> ⚠ NO BINARY64 VALUE IS PRINTED. §15.4.1's last paragraph gives a function with an equivalent arithmetic
      *> expression "an implementor-defined approximation of the value of that expression" when native arithmetic
      *> is in effect, and 1/100000 is not a binary fraction, so the float legs assert the RULE-derived facts —
      *> that a legal argument is admitted and lands strictly between 1 and 2, and that an illegal one raises —
      *> never a digit string. The fixed-point legs are exact and carry the values, derived as in the twin:
      *> 3661.12345678 / 100000 = 0.0366112345678, + 1 = 1.0366112345678.
      *>
      *> ARMED, NOT ASSUMED: G3 raises from the binary64 carrier and the declarative reports it; the abandoned
      *> COMPUTE leaves the receiver at its preset 9. G4 is the complement — the same carrier, a legal value, no
      *> raise — because a guard that fired early would be exactly as wrong as one that never fired.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB620CAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   PIC 9(5)V9(8) VALUE 3661.12345678.
       01 WS-BAD PIC 9(5)V9(8) VALUE 86400.
       01 F3     USAGE COMP-2 VALUE 86400.
       01 F4     USAGE COMP-2 VALUE 3661.12345678.
       01 WS-A   PIC 9V9(13).
       01 WS-B   PIC 9V9(13).
       01 WS-C   PIC 9V9(13).
       01 WS-F   PIC 9V9(13).
       01 ES     PIC X(20).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           MOVE FUNCTION EXCEPTION-STATUS TO ES
           DISPLAY "ARMED=" ES.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> A/B/C — the exact carrier, the same value through an arithmetic expression, and §15.17.4 r1's
      *> equivalent arithmetic expression written out. Under native these are all exact fixed-point.
           COMPUTE WS-A = FUNCTION COMBINED-DATETIME(1, WS-S)
           DISPLAY "A=" WS-A
           COMPUTE WS-B = FUNCTION COMBINED-DATETIME(1, WS-S + 0)
           DISPLAY "B=" WS-B
           COMPUTE WS-C = 1 + ((WS-S + 0) / 100000)
           DISPLAY "C=" WS-C
      *> AGREE — the two argument SHAPES are one value, as they are under a standard mode.
           IF FUNCTION COMBINED-DATETIME(1, WS-S) =
              FUNCTION COMBINED-DATETIME(1, WS-S + 0)
               DISPLAY "AGREE=OK"
           ELSE
               DISPLAY "AGREE=BAD"
           END-IF
      *> G1/G2 — the §15.17.3 r2 screen from the exact carrier and from an arithmetic expression.
           MOVE 9 TO WS-A
           COMPUTE WS-A = FUNCTION COMBINED-DATETIME(1, WS-BAD)
           DISPLAY "G1=" WS-A
           MOVE 9 TO WS-B
           COMPUTE WS-B = FUNCTION COMBINED-DATETIME(1, WS-BAD + 0)
           DISPLAY "G2=" WS-B
      *> G3 — and from the BINARY64 carrier: the same value, held in a COMP-2 item.
           MOVE 9 TO WS-F
           COMPUTE WS-F = FUNCTION COMBINED-DATETIME(1, F3)
           DISPLAY "G3=" WS-F
      *> G4 — the complement: a LEGAL float argument is admitted, and §15.17.4 r1 places its value strictly
      *> between 1 (the integer date) and 2 (that date plus 100000/100000 seconds, which r2 excludes).
           COMPUTE WS-F = FUNCTION COMBINED-DATETIME(1, F4)
           IF WS-F > 1 AND WS-F < 2
               DISPLAY "G4=OK"
           ELSE
               DISPLAY "G4=BAD"
           END-IF
           STOP RUN.
       END PROGRAM PB620CAR.
