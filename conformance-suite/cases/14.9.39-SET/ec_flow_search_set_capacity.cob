*>--- conformance-case
*> id: ec_flow_search_set_capacity
*> rules: [GR-14.9.39.4-31]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.4 GR31: "This statement shall not be executed during the execution of a SEARCH statement
      *> referring to the same table. If this rule is violated, the EC-FLOW-SEARCH exception condition is set to
      *> exist AND THE SET STATEMENT IS NOT EXECUTED." Table 13: Fatal, so the declarative runs and RESUME AT
      *> NEXT STATEMENT (§14.9.33) keeps the run unit alive.
      *>
      *> The SET CAPACITY sits inside the SEARCH's own WHEN body, so it targets the table being searched. GR31
      *> names the lenient outcome outright, which is why checking-OFF returns having done nothing instead of
      *> terminating: the SET is simply not executed. CAP is displayed afterwards to prove exactly that — the
      *> capacity is unchanged whether or not the condition was raised. The check is on the VALUE, not on a
      *> DISPLAY of the register: an implicitly-defined CAPACITY register's picture is implementor-defined
      *> (§13.18.38 SR30), so its rendering width is not a spec-derived expectation and must not be asserted.
      >>TURN EC-FLOW-SEARCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECFLOWS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D.
          05 E PIC 9(2) OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 8
             INDEXED BY IX.
       01 I   PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-SEARCH.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET CAP TO 3.
           MOVE 7 TO E (1).
           SEARCH E
               WHEN E (1) = 7
                   SET CAP TO 6
           END-SEARCH.
           IF CAP = 3
               DISPLAY "CAP-UNCHANGED"
           ELSE
               DISPLAY "CAP-CHANGED"
           END-IF.
           STOP RUN.
