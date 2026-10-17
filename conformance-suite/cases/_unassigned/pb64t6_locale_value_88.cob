*>--- conformance-case
*> id: pb64t6_locale_value_88
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB64 T6 — the format-2 item's VALUE clause, level-88 membership, INITIALIZE, REDEFINES width and the
      *> de-edit acceptance rule. IN THE 2023 CORPUS because a NUMERIC-LITERAL VALUE for a numeric-edited item
      *> is itself a COBOL-2023 introduction (ISO 13.18.63 SR6; Annex E.3.3 item 43 - the value-numeric-edited
      *> gate, COBOLNET0900 below 2023); the format-2 semantics under test are 2002+.
      *> de-edit acceptance rule. The VALUE image is composed AT RUN TIME (ISO 13.18.40.5 r11 + 14.6.6 r6 make
      *> the locale the one current at editing time - no compile-time image exists), and the level-88 membership
      *> value composes the same way (13.18.63.3 SR6 converts per the MOVE rules; comparing raw literal text is
      *> the PB97 defect). The REDEFINES sees exactly SIZE characters (13.18.40.4 GR17 - the picture is NOT the
      *> field size). INITIALIZE gives a numeric-edited member the edited image of ZERO (14.9.20). A ref-mod/
      *> redefines write of arbitrary text then makes the de-editing MOVE raise EC-DATA-INCOMPATIBLE
      *> (14.6.13.2 r4 - "not a possible result for any editing operation in that data item").
      *> Hand-derived (en-US): VALUE 10 into $Z9.99 SIZE 8 -> "  $10.00"; MOVE 20.50 flips the 88 off;
      *> INITIALIZE -> zero edits to "  $ 0.00" (the Z suppresses the leading zero).
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6VL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 V PIC $Z9.99 LOCALE IS US SIZE IS 8 VALUE 10.
             88 V-TEN VALUE 10.
          05 X REDEFINES V PIC X(8).
       01 W PIC S9(3)V99 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "[" V "]"
           IF V-TEN DISPLAY "88 OK" ELSE DISPLAY "88 BAD" END-IF
           DISPLAY "[" X "]"
           MOVE 20.50 TO V
           IF V-TEN DISPLAY "88 BAD2" ELSE DISPLAY "88 OFF OK" END-IF
           INITIALIZE G
           DISPLAY "[" V "]"
           MOVE "GARBAGE!" TO X
           MOVE V TO W
           IF W = 1 DISPLAY "W UNCHANGED" ELSE DISPLAY "W BAD " W END-IF
           STOP RUN.
