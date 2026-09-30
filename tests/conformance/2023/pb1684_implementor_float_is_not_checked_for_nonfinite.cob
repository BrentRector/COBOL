      *> PB1684 - ISO 14.6.13.2 3): EC-DATA-NOT-FINITE is scoped to a sending operand
      *>   "described with a standard floating-point usage" (3.166/3.167: FLOAT-BINARY-n,
      *>   FLOAT-DECIMAL-n). FLOAT-LONG and COMP-2 are implementor usages, so an infinite
      *>   one sends without the exception; the declarative never runs.
       >>TURN EC-DATA-NOT-FINITE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1684P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-FL   USAGE FLOAT-LONG.
       01 WS-C2   USAGE COMP-2.
       01 WS-R    USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-NOT-FINITE.
       H-P.
           DISPLAY "CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE WS-FL = 1.0E300 * 1.0E300.
           COMPUTE WS-C2 = 1.0E300 * 1.0E300.
           COMPUTE WS-R = WS-FL + 1.
           IF WS-C2 > 1.0 DISPLAY "GT" END-IF.
           DISPLAY "DONE".
           STOP RUN.
