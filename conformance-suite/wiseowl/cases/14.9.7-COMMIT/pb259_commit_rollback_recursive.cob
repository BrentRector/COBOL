*>--- conformance-case
*> id: pb259_commit_rollback_recursive
*> rules: [GR-14.9.7.4-2, GR-14.9.36.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB259 — the RECURSIVE-runtime-element leg of §14.9.7.4 GR2
      *> / §14.9.36.4 GR2 under the declined commit and rollback facility.
      *> GR2 sets EC-FLOW-COMMIT (EC-FLOW-ROLLBACK) when the statement "is
      *> attempted to be executed under the control of a recursive runtime
      *> element"; the module is Annex A.4.3's OPTIONAL one (items 1, 4,
      *> 5), Not claimed in docs/CONFORMANCE.md §5, A.4.1 makes its
      *> exception conditions optional with it, and §14.6.13.1.1 does not
      *> require a level-3 condition of such an element to be raised. A
      *> COMMIT written IN a recursive source element is rejected
      *> (§14.9.7.3 SR1, COBOLNET1690, negative/pb137-commit-recursive),
      *> so the reach left is DYNAMIC: PB259RSB is a separately compiled,
      *> non-recursive program called while the RECURSIVE PB259REC is
      *> active. Both statements compile (COBOLNET1579) and behave as
      *> CONTINUE (§14.9.7.4 GR1, §14.9.36.4 GR1, no active APPLY COMMIT
      *> clause); under EC-ALL checking no condition is raised, so both
      *> EXCEPTION-STATUS reads are blank and control returns normally.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB259REC RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ST PIC X(31).
       PROCEDURE DIVISION.
           CALL 'PB259RSB'.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST.
           DISPLAY 'BACK ST=[' WS-ST ']'.
           STOP RUN.
       END PROGRAM PB259REC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB259RSB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-ST2 PIC X(31).
       PROCEDURE DIVISION.
           COMMIT.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST2.
           DISPLAY 'COMMIT ST=[' WS-ST2 ']'.
           ROLLBACK.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-ST2.
           DISPLAY 'ROLLBACK ST=[' WS-ST2 ']'.
           GOBACK.
       END PROGRAM PB259RSB.
