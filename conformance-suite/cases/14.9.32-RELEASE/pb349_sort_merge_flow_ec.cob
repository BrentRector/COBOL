*>--- conformance-case
*> id: pb349_sort_merge_flow_ec
*> rules: [GR-14.9.32.4-1, GR-14.9.34.4-1, GR-14.9.34.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB349 -- the sort-merge FLOW exception conditions, CHECKED.
      *> ISO §14.9.32.4 GR1: "A RELEASE statement may be executed only when it is
      *> within the range of an input procedure being executed by a SORT statement
      *> that references the file-name associated with record-name-1. If it is
      *> executed at any other time, the EC-FLOW-RELEASE exception condition is set
      *> to exist."
      *> §14.9.34.4 GR1: "A RETURN statement may be executed only when it is within
      *> the range of an output procedure being executed by a MERGE or SORT
      *> statement that references file-name-1. If it is executed at any other
      *> time, the EC-FLOW-RETURN exception condition is set to exist."
      *> §14.9.34.4 GR3: "After the execution of imperative-statement-1 in the AT
      *> END phrase, no RETURN statement may be executed as part of the current
      *> output procedure. If such a RETURN statement is executed, the
      *> EC-SORT-MERGE-RETURN exception condition is set to exist".
      *> All three are Table 13 Fatal; under >>TURN ... CHECKING ON the format-3
      *> declarative runs (§14.6.13.1.3 item 5) and RESUME AT NEXT STATEMENT
      *> (§14.9.33.4 GR2) continues after the offending statement, which is NOT
      *> executed: the raise precedes its action.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, in execution order --
      *>   HANDLED=EC-FLOW-RELEASE   MAIN's RELEASE: no SORT is executing.
      *>   HANDLED=EC-FLOW-RETURN    MAIN's RETURN: no SORT/MERGE is executing; the
      *>                             RETURN is abandoned, so neither AT END nor
      *>                             NOT AT END runs (no "MAIN-AT-END" line).
      *>   R=AAA / R=BBB             OUT-P returns the two records IN-P released,
      *>                             ascending on SR-KEY (§14.9.40.4 GR8 a);
      *>                             the stranded ZZZ is not among them.
      *>   AT-END                    the third RETURN finds no next record (GR3).
      *>   HANDLED=EC-SORT-MERGE-RETURN   the fourth RETURN, after AT END ran.
      *>   AFTER-SORT N=2            two records were made available.
      *> Negative control: no condition is raised for the RELEASEs inside IN-P or the
      *> first three RETURNs inside OUT-P, although all three names are enabled.
      >>TURN EC-FLOW-RELEASE EC-FLOW-RETURN EC-SORT-MERGE-RETURN CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB349FLOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb349-flow.srt".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SRT-REC.
          05 SR-KEY  PIC X(3).
          05 SR-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       01 WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-RELEASE
               EC-FLOW-RETURN EC-SORT-MERGE-RETURN.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "ZZZ" TO SR-KEY.
           MOVE "zzzzz" TO SR-DATA.
           RELEASE SRT-REC.
           RETURN SF
               AT END DISPLAY "MAIN-AT-END"
               NOT AT END DISPLAY "MAIN-NOT-AT-END"
           END-RETURN.
           SORT SF ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IN-P
               OUTPUT PROCEDURE OUT-P.
           DISPLAY "AFTER-SORT N=" WS-N.
           STOP RUN.
       IN-P.
           MOVE "BBB" TO SR-KEY.
           MOVE "bbbbb" TO SR-DATA.
           RELEASE SRT-REC.
           MOVE "AAA" TO SR-KEY.
           MOVE "aaaaa" TO SR-DATA.
           RELEASE SRT-REC.
       OUT-P.
           PERFORM UNTIL WS-EOF = 1
               RETURN SF
                   AT END
                       MOVE 1 TO WS-EOF
                       DISPLAY "AT-END"
                   NOT AT END
                       ADD 1 TO WS-N
                       DISPLAY "R=" SR-KEY
               END-RETURN
           END-PERFORM.
           RETURN SF
               AT END DISPLAY "SECOND-AT-END"
           END-RETURN.
