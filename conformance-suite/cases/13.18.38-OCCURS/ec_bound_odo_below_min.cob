*>--- conformance-case
*> id: ec_bound_odo_below_min
*> rules: [GR-13.18.38.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.38.4 GR7: "At the time the subject of entry is referenced … the value of the data item
      *> referenced by data-name-1 shall fall within the bounds from integer-1 through integer-2. If the value
      *> of the data item does not fall within the specified bounds, the EC-BOUND-ODO exception condition is
      *> set to exist." BOTH ends are bounds — this golden exercises the LOWER one.
      *>
      *> N is 1 against OCCURS 2 TO 5, so the control value is below integer-1. Until this fix the runtime
      *> extent computation clamped with a hardcoded floor of 0 and never saw integer-1 at all, so a
      *> below-minimum DEPENDING value was silent at every checking state; integer-1 is now carried from the
      *> OCCURS spec through the Place and into the extent call. Table 13: Fatal, so RESUME AT NEXT STATEMENT
      *> keeps the run unit alive. GR7's closing sentence — content beyond the count is undefined — is what
      *> makes the checking-OFF clamp a conforming implementor choice.
      >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECODOMIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9 VALUE 1.
          05 T PIC X OCCURS 2 TO 5 TIMES DEPENDING ON N.
       01 IMG PIC X(6) VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-ODO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE G TO IMG.
           DISPLAY "AFTER".
           STOP RUN.
