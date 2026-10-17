*>--- conformance-case
*> id: pb395_subtract_incompatible_sender
*> rules: [GR-14.9.44.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.44.4 GR6 (SUBTRACT) is a POINTER rule — "The following specifications apply: 14.7.4 ROUNDED
      *> phrase, 14.7.5 SIZE ERROR phrase and size error condition, 14.7.6 CORRESPONDING phrase, 14.7.7 Arithmetic
      *> statements, 14.6.13.2 Incompatible data" — so it is verified by verifying that each referenced
      *> specification applies ON SUBTRACT. §14.7.4 / §14.7.5 / §14.7.6 / §14.7.7 are pinned elsewhere
      *> (conformance:85/optional_on_size_error for the SIZE ERROR leg); THIS golden is the §14.6.13.2 leg, which
      *> had no covering case for any verb.
      *>
      *> §14.6.13.2 rule 2: "When the content of a numeric sending item that is not described with a standard
      *> floating-point usage is referenced during the execution of a statement and the content of that sending
      *> operand would evaluate to false in a numeric class condition, the result of the reference is undefined
      *> and an EC-DATA-INCOMPATIBLE exception condition is set to exist" — the rule hangs on the SENDING-OPERAND
      *> REFERENCE, not on a statement kind, so SUBTRACT raises it exactly as MOVE does. Table 13 makes it FATAL;
      *> the declarative's RESUME AT NEXT STATEMENT (§14.9.33.4 GR2) abandons the SUBTRACT and keeps the run unit
      *> alive. The rule leaves the result undefined, so T is NOT displayed until it is re-established.
      *>
      *> The second SUBTRACT is the control: the same window holding a valid numeric image raises nothing and
      *> stores 100 - 7 = 93, so a compiler that simply raised on every SUBTRACT would fail here.
      *>
      *> 2002 is the introducing edition of the observable (>>TURN is a COBOL-2002 introduction, §7.3.25); see
      *> conformance:negative/pb395-turn-data-incompatible-below-2002 for that gate.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB395SUBINC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 W-X PIC X(2) VALUE "AB".
       01 R REDEFINES G.
          05 W-N PIC 9(2).
       01 T PIC 9(4) VALUE 100.
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
           DISPLAY "SENDER=[" W-X "]".
           SUBTRACT W-N FROM T.
           DISPLAY "AFTER-SUBTRACT".
           MOVE 7 TO W-N.
           MOVE 100 TO T.
           SUBTRACT W-N FROM T.
           DISPLAY "T=" T.
           STOP RUN.
