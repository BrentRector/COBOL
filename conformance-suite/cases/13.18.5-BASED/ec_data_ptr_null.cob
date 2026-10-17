*>--- conformance-case
*> id: ec_data_ptr_null
*> rules: [GR-13.18.5.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.5.4 GR3: "If the subject of the entry or any item subordinate to it is referenced directly
      *> or indirectly while its address is NULL, the EC-DATA-PTR-NULL exception condition is set to exist."
      *> Table 13: Fatal — so the USE declarative runs and RESUME AT NEXT STATEMENT (§14.9.33) is what keeps the
      *> run unit alive past the failed reference; without it §14.6.13.1.3 #5 terminates abnormally.
      *>
      *> B is BASED and its address is never SET, so DISPLAY B dereferences a NULL data-address pointer. Before
      *> this fix CobolPtr.Deref threw a CobolFatalException directly without setting the last-exception status
      *> and with no emitted statement guard, so the declarative could never select and the run unit died.
      *>
      *> ⚠ The dereference throws whether or not checking is enabled — the owner's decided rule, because
      *> §13.18.5.4 GR3/GR4 name NO outcome for the unchecked case and Deref must return a StorageCell, so
      *> "lenient" could only mean continuing on a fabricated cell. Every surveyed COBOL hard-stops here. What
      *> checking ON adds is the NAMED condition and the chance to handle it, which is what this golden proves.
      >>TURN EC-DATA-PTR-NULL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECPTRNUL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC X(4) BASED.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-PTR-NULL.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY B.
           DISPLAY "AFTER".
           STOP RUN.
