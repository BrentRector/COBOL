*>--- conformance-case
*> id: ec_bound_ref_mod
*> rules: [GR-8.4.3.3.4-4, GR-8.4.3.3.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> EC-BOUND-REF-MOD (ISO §8.4.3.3.4, spec :7089; Table 13 Fatal): a reference
      *> modification whose leftmost-position or length is out of range — here
      *> WS-X(7:2) on a 5-position item, leftmost 7 > 5 — raises the fatal
      *> EC-BOUND-REF-MOD during the MOVE's source evaluation (before the store, so
      *> WS-Y is unchanged). Under >>TURN EC-BOUND-REF-MOD CHECKING ON the USE AFTER
      *> EXCEPTION CONDITION declarative selects (§14.9.49), reports the condition via
      *> FUNCTION EXCEPTION-STATUS (§15.33), and RESUME AT NEXT STATEMENT (§14.9.33)
      *> continues past the aborted MOVE. With checking OFF the out-of-range ref-mod
      *> clamps/space-pads (the lenient default), so this is a checking-ON observable.
      >>TURN EC-BOUND-REF-MOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-BOUND-RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(5) VALUE "HELLO".
       01 WS-Y PIC X(2) VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-REF-MOD.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE WS-X (7:2) TO WS-Y.
           DISPLAY "Y=[" WS-Y "]".
           STOP RUN.
