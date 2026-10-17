      *> kb/Work PB3149 - the MCS "no requester" exception-name is
      *> spelled as 14.6.13.1.6 Table 13 spells it: the row
      *> "| EC-MCS-NO-REQUESTER | NF | RECEIVE statement |". Annex E.3.2
      *> 3) (informative) prints EC-MCS-NO-REQUESTOR, which is not a
      *> name (docs/CONFORMANCE.md D-MCS-NR). The sibling keeps Table
      *> 13's -OR spelling, EC-MCS-REQUESTOR-FAILED. Both are accepted
      *> inert with the COBOLNET1578 warning (PB374), and a RAISE
      *> raises each: 14.9.29.4 GR1 "the associated exception
      *> condition is raised". Both are NONFATAL (Table 13 "NF"), so
      *> control continues after the RAISE.
       >>TURN EC-MCS-NO-REQUESTER CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3149EC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-MCS-NO-REQUESTER.
       D1-P.
           DISPLAY "DECLARATIVE "
               FUNCTION TRIM (FUNCTION EXCEPTION-STATUS).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           RAISE EXCEPTION EC-MCS-NO-REQUESTER.
           DISPLAY "AFTER RAISE".
           PERFORM
               RAISE EXCEPTION EC-MCS-REQUESTOR-FAILED
           WHEN EC-MCS-REQUESTOR-FAILED
               DISPLAY "WHEN "
                   FUNCTION TRIM (FUNCTION EXCEPTION-STATUS)
           END-PERFORM.
           DISPLAY "DONE".
           STOP RUN.
