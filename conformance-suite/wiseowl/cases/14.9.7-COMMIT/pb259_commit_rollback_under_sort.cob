*>--- conformance-case
*> id: pb259_commit_rollback_under_sort
*> rules: [GR-14.9.7.4-2, GR-14.9.36.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB259 — the DYNAMIC half of §14.9.7.4 GR2 / §14.9.36.4 GR2
      *> under the declined commit and rollback facility. GR2: "If this
      *> statement is attempted to be executed under the control of a
      *> recursive runtime element or a file SORT or MERGE statement, then
      *> the exception condition EC-FLOW-COMMIT is set to exist" (and
      *> EC-FLOW-ROLLBACK for ROLLBACK). §9.1.18.7 states the same pair.
      *> The facility is the OPTIONAL module of Annex A.4.3 (items 1, 4
      *> and 5), recorded Not claimed in docs/CONFORMANCE.md §5, and A.4.1
      *> makes its "exception conditions" optional with it; §14.6.13.1.1:
      *> "The implementor is not required to raise any exception
      *> conditions for level-3 exception-names" of such an element. The
      *> STATIC reach of GR2 (a COMMIT/ROLLBACK written inside the SORT
      *> procedure's own range) is rejected at bind time, COBOLNET1690
      *> (negative/pb137-commit-in-sort-proc). This golden pins the
      *> DYNAMIC reach: HELPER lies OUTSIDE both procedure ranges and is
      *> PERFORMed from them while the SORT is active, so it compiles
      *> (COBOLNET1579 at each site) and each statement behaves as
      *> CONTINUE, which with no active APPLY COMMIT clause is its whole
      *> effect (§14.9.7.4 GR1, §14.9.36.4 GR1). Under EC-ALL checking a
      *> raised EC-FLOW-COMMIT (Fatal, §14.6.13.1.6) would end the run;
      *> the declined posture raises none, so EXCEPTION-STATUS stays
      *> blank and the SORT completes in key order.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB259SRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO 'PB259SWK'.
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC X(31).
       01 WS-EOF PIC X VALUE 'N'.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           DISPLAY 'SORTED'.
           STOP RUN.
       FEED.
           MOVE 'BBBB' TO SR.
           RELEASE SR.
           PERFORM HELPER.
           MOVE 'AAAA' TO SR.
           RELEASE SR.
       DRAIN.
           PERFORM HELPER.
           PERFORM UNTIL WS-EOF = 'Y'
               RETURN SF
                   AT END MOVE 'Y' TO WS-EOF
                   NOT AT END DISPLAY 'REC ' SR
               END-RETURN
           END-PERFORM.
       HELPER.
           COMMIT.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST.
           DISPLAY 'COMMIT ST=[' WS-ST ']'.
           ROLLBACK.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST.
           DISPLAY 'ROLLBACK ST=[' WS-ST ']'.
