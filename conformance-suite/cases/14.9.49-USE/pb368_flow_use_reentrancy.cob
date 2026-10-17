*>--- conformance-case
*> id: pb368_flow_use_reentrancy
*> rules: [GR-14.9.49.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.4 GR2 (ALL FORMATS) — "During the execution of a
      *> USE procedure, if a statement raises an exception condition
      *> that would cause the execution of a USE procedure that had
      *> previously been activated and had not yet returned control to
      *> the activating entity, the EC-FLOW-USE exception condition is
      *> set to exist."
      *>
      *> THE RAISE IS THE RULE'S WHOLE NORMATIVE CONTENT.  Declining to
      *> re-enter the active USE procedure is what an implementation has
      *> to do anyway (§14.6.13.1.3 #8 governs it when checking is off);
      *> the condition is what lets a program SEE its own declarative
      *> recursion, and without it the Table 13 (§14.6.13.1.6) Fatal
      *> default — "EC-FLOW-USE | Fatal | A USE statement caused another
      *> to be executed" — can never fire either.  kb/Work PB368.
      *>
      *> WHAT THIS PROGRAM PINS.  MAIN's OPEN of an absent file sets I-O
      *> status 35, which selects the file-scoped format-1 declarative D1
      *> (GR3 a) / GR6 a)).  D1 then OPENs the SAME file, whose failure
      *> would select D1 again — D1 has not returned control — so GR2
      *> sets EC-FLOW-USE.  Checking for it is enabled by the >>TURN
      *> directive (§14.6.13.1.1: "it is raised only if checking for that
      *> exception condition is enabled"), so §14.6.13.1.3 #5 applies and
      *> the GR3 e) tier — a format-3 USE whose exception-name-1 is a
      *> level-3 name matching the raised condition — selects DFU.
      *> FUNCTION EXCEPTION-STATUS (§15.32.3) inside DFU names the
      *> condition that selected it.  DFU's RESUME AT NEXT STATEMENT
      *> (§14.9.33.4 GR2) returns control to the statement following the
      *> inner OPEN, so D1 finishes and MAIN continues — the declarative
      *> did NOT complete normally, which is why #5's "the execution of
      *> the run unit is terminated abnormally" does not apply here.
      *> (The no-declarative default is the #7 termination, pinned by
      *> FlowUseReentrancyTests.)
      *>
      *> FS1 reads 35 after the inner OPEN because the inner OPEN failed
      *> on the same absent file (§9.1.13.1 — permanent error, optional
      *> file not present).
      *>
      *> EDITION: the exception-condition machinery this needs — the
      *> >>TURN directive (§7.3.25), the format-3 USE (§14.9.49.2), the
      *> RESUME statement (§14.9.33) and the EC-FLOW-USE name itself —
      *> is a COBOL-2002 introduction; the REJECT half below it is
      *> conformance:cases/14.9.49-USE/pb368-flow-use-name-below-2002.
       >>TURN EC-FLOW-USE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB368FU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb368fu-absent.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC   PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS1      PIC XX VALUE "00".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       D1-P.
           DISPLAY "D1-ENTER".
           OPEN INPUT F1.
           DISPLAY "D1-EXIT FS=" FS1.
       DFU SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-USE.
       DFU-P.
           DISPLAY "FLOW-USE-HANDLER " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN INPUT F1.
           DISPLAY "AFTER FS=" FS1.
           STOP RUN.
