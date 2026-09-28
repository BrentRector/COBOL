      *> kb/Work PB1268 — EC-BOUND-ODO AT EVERY REFERENCE §13.18.38.4 GR7 NAMES: the subject of the entry, an item
      *> subordinate to it, and an item superordinate to it.
      *>
      *> THE RULE, --check validated:
      *>   cite.py --check 13.18.38.4 "If the value of the data item does not fall within the specified bounds, the
      *>     EC-BOUND-ODO exception condition is set to exist" -> OK §13.18.38.4 7) — whose first sentence reads "At
      *>     the time the subject of entry is referenced or any data item subordinate or superordinate to the
      *>     subject of entry is referenced, the value of the data item referenced by data-name-1 shall fall within
      *>     the bounds from integer-1 through integer-2".
      *> >>TURN is COBOL-2002 (§7.3.25); negative/w70c-pb1268-turn-bound-odo-below-2002 pins its refusal at 85.
      *>
      *> WHY EACH LEG CAN FAIL (the pre-PB1268 build raised only for the superordinate group, leg 2):
      *>   1  N = 7 (> integer-2 5): DISPLAY TX(1), TX SUBORDINATE to the subject T  -> RAISED EC-BOUND-ODO
      *>   2  N = 7: MOVE G TO IMG, G SUPERORDINATE to T                            -> RAISED EC-BOUND-ODO
      *>   3  N = 1 (< integer-1 2): MOVE "Z" TO T(1), the SUBJECT itself              -> RAISED EC-BOUND-ODO
      *>   4  N = 3 (in bounds): DISPLAY TX(2) -> B, no condition — the check is a bound test, not a raise on
      *>      every reference.
      *>   5  CNT = 03.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CPB1268.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99 VALUE 5.
       01 G.
          05 H PIC X.
          05 T OCCURS 2 TO 5 DEPENDING ON N.
             10 TX PIC X.
             10 TE PIC 9.
       01 IMG PIC X(40).
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-ODO.
       D1P.
           ADD 1 TO CNT
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "HA1B2C3D4E5" TO G
           MOVE 7 TO N
           DISPLAY "1 " TX(1)
           MOVE G TO IMG
           MOVE 1 TO N
           MOVE "Z" TO T(1)
           MOVE 3 TO N
           DISPLAY "4 " TX(2)
           DISPLAY "5 CNT=" CNT
           STOP RUN.
